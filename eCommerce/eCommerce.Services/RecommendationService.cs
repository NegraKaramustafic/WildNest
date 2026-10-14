using eCommerce.Model.Enums;
using eCommerce.Model;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace eCommerce.Services
{
    public class RecommendationService : IRecommendationService
    {

        private readonly ECommerceDbContext _dbContext;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly RecommendationModelCache _modelCache;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            ECommerceDbContext dbContext,
            IAuthenticatedUserAccessor userAccessor,
            RecommendationModelCache modelCache,
            ILogger<RecommendationService> logger)
        {
            _dbContext = dbContext;
            _userAccessor = userAccessor;
            _modelCache = modelCache;
            _logger = logger;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(int? limit)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new UnauthorizedAccessException("User id claim is missing.");
            var take = ResolveLimit(limit);

            if (_modelCache.NeedsTraining)
            {
                await TrainFromDatabaseAsync();
            }

            var state = _modelCache.Capture();
            var context = await LoadScoringContextAsync(userId);
            var engine = CreatePredictionEngine(state);

            var scored = new List<(RecommendationItemResponse Item, float MfScore)>();
            foreach (var activity in context.Candidates)
            {
                if (activity.Status != ActivityStatus.Active)
                {
                    continue;
                }

                var mf = PredictScore(engine, userId, activity.Id);
                scored.Add((BuildItem(activity, context, mf), mf ?? 0));
            }

            var mfMedian = Median(scored.Where(s => s.MfScore > 0).Select(s => (double)s.MfScore).ToList());

            foreach (var (item, mfScore) in scored)
            {
                if (engine == null || mfScore <= 0 || mfScore < mfMedian)
                {
                    item.Reasons.RemoveAll(r => r.Code == RecommendationWeights.Collaborative);
                }

                if (item.Reasons.Count == 0)
                {
                    item.Reasons.Add(new RecommendationReasonResponse
                    {
                        Code = RecommendationWeights.Available,
                        Text = "An activity you have not booked yet"
                    });
                }
            }

            var items = scored
                .OrderByDescending(s => s.Item.Score)
                .ThenBy(s => s.Item.Name)
                .Take(take)
                .Select(s => s.Item)
                .ToList();

            return new RecommendationResponse
            {
                GeneratedAt = DateTime.UtcNow,
                ModelTrainedAt = state.TrainedAtUtc,
                TrainingRowCount = state.TrainingRowCount,
                Items = items
            };
        }

        public async Task<RecommendationRetrainResponse> RetrainAsync()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can retrain the recommendation model.");
            }

            await TrainFromDatabaseAsync();
            var state = _modelCache.Capture();
            return new RecommendationRetrainResponse
            {
                TrainedAt = state.TrainedAtUtc ?? DateTime.UtcNow,
                TrainingRowCount = state.TrainingRowCount
            };
        }

        private static int ResolveLimit(int? limit)
        {
            var value = limit ?? RecommendationWeights.DefaultLimit;
            if (value < 1 || value > RecommendationWeights.MaxLimit)
            {
                throw new ClientException($"limit must be between 1 and {RecommendationWeights.MaxLimit}.");
            }

            return value;
        }

        private async Task TrainFromDatabaseAsync()
        {
            var rows = await LoadTrainingRowsAsync();
            if (rows.Count < RecommendationWeights.MinTrainingRows)
            {
                _logger.LogInformation(
                    "Skipping MatrixFactorization training; only {RowCount} rating rows (minimum {Min}).",
                    rows.Count,
                    RecommendationWeights.MinTrainingRows);
                _modelCache.SetTrained(null, null, rows.Count);
                return;
            }

            try
            {
                var mlContext = new MLContext(seed: 1);
                var data = mlContext.Data.LoadFromEnumerable(rows);
                var options = new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = "UserIdEncoded",
                    MatrixRowIndexColumnName = "ActivityIdEncoded",
                    LabelColumnName = nameof(ActivityRatingRow.Label),
                    NumberOfIterations = 20,
                    ApproximationRank = 4,
                    Quiet = true
                };

                var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                        outputColumnName: "UserIdEncoded",
                        inputColumnName: nameof(ActivityRatingRow.UserId))
                    .Append(mlContext.Transforms.Conversion.MapValueToKey(
                        outputColumnName: "ActivityIdEncoded",
                        inputColumnName: nameof(ActivityRatingRow.ActivityId)))
                    .Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));

                var model = pipeline.Fit(data);
                _modelCache.SetTrained(model, mlContext, rows.Count);
                _logger.LogInformation("Trained recommendation model on {RowCount} rating rows.", rows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MatrixFactorization training failed; scoring will use content signals only.");
                _modelCache.SetTrained(null, null, rows.Count);
            }
        }

        private async Task<List<ActivityRatingRow>> LoadTrainingRowsAsync()
        {
            var reviews = await _dbContext.Reviews
                .AsNoTracking()
                .Select(r => new { r.UserId, r.ActivityId, r.Rating })
                .ToListAsync();

            var reviewed = reviews
                .Select(r => (r.UserId, r.ActivityId))
                .ToHashSet();

            var implicitReservations = await _dbContext.Reservations
                .AsNoTracking()
                .Where(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed)
                .Select(r => new { r.UserId, r.Schedule.ActivityId })
                .Distinct()
                .ToListAsync();

            var rows = reviews
                .Select(r => new ActivityRatingRow
                {
                    UserId = r.UserId,
                    ActivityId = r.ActivityId,
                    Label = r.Rating
                })
                .ToList();

            foreach (var reservation in implicitReservations)
            {
                if (reviewed.Contains((reservation.UserId, reservation.ActivityId)))
                {
                    continue;
                }

                rows.Add(new ActivityRatingRow
                {
                    UserId = reservation.UserId,
                    ActivityId = reservation.ActivityId,
                    Label = RecommendationWeights.ImplicitReservationLabel
                });
            }

            return rows;
        }

        private async Task<ScoringContext> LoadScoringContextAsync(int userId)
        {
            var activities = await _dbContext.Activities
                .AsNoTracking()
                .Include(a => a.Animal)
                .Include(a => a.ActivityType)
                .Where(a => a.Status == ActivityStatus.Active)
                .ToListAsync();

            var reviews = await _dbContext.Reviews
                .AsNoTracking()
                .Include(r => r.Activity)
                    .ThenInclude(a => a.Animal)
                .ToListAsync();

            var qualifyingReservations = await _dbContext.Reservations
                .AsNoTracking()
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Activity)
                        .ThenInclude(a => a.Animal)
                .Where(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Completed)
                .ToListAsync();

            var adoptions = await _dbContext.Adoptions
                .AsNoTracking()
                .Where(a => a.Status == AdoptionStatus.Active)
                .Select(a => new { a.UserId, a.AnimalId })
                .ToListAsync();

            var seenActivityIds = qualifyingReservations
                .Where(r => r.UserId == userId)
                .Select(r => r.Schedule.ActivityId)
                .ToHashSet();

            var reviewStats = reviews
                .GroupBy(r => r.ActivityId)
                .ToDictionary(
                    g => g.Key,
                    g => (Avg: g.Average(r => (double)r.Rating), Count: g.Count()));

            var popularity = qualifyingReservations
                .GroupBy(r => r.Schedule.ActivityId)
                .ToDictionary(g => g.Key, g => g.Count());

            var userTypes = new HashSet<int>();
            var userAnimalIds = new HashSet<int>();
            var userCategoryIds = new HashSet<int>();

            foreach (var review in reviews.Where(r => r.UserId == userId && r.Activity != null))
            {
                AddUserAffinity(userTypes, userAnimalIds, userCategoryIds, review.Activity);
            }

            foreach (var reservation in qualifyingReservations.Where(r => r.UserId == userId && r.Schedule.Activity != null))
            {
                AddUserAffinity(userTypes, userAnimalIds, userCategoryIds, reservation.Schedule.Activity);
            }

            var adoptedAnimalIds = adoptions
                .Where(a => a.UserId == userId)
                .Select(a => a.AnimalId)
                .ToHashSet();

            var adoptedIds = adoptedAnimalIds.ToList();
            var adoptedCategories = adoptedIds.Count == 0
                ? new Dictionary<int, int>()
                : await _dbContext.Animals.AsNoTracking()
                    .Where(a => adoptedIds.Contains(a.Id))
                    .Select(a => new { a.Id, a.CategoryId })
                    .ToDictionaryAsync(a => a.Id, a => a.CategoryId);

            foreach (var animalId in adoptedAnimalIds)
            {
                userAnimalIds.Add(animalId);
                if (adoptedCategories.TryGetValue(animalId, out var categoryId))
                {
                    userCategoryIds.Add(categoryId);
                }
            }

            var globalMean = reviews.Count > 0
                ? reviews.Average(r => (double)r.Rating)
                : 3.0;

            return new ScoringContext
            {
                Candidates = activities
                    .Where(a => a.Status == ActivityStatus.Active)
                    .Where(a => !seenActivityIds.Contains(a.Id))
                    .ToList(),
                ReviewStats = reviewStats,
                Popularity = popularity,
                UserTypes = userTypes,
                UserAnimalIds = userAnimalIds,
                UserCategoryIds = userCategoryIds,
                UserAdoptedAnimalIds = adoptedAnimalIds,
                GlobalMeanRating = globalMean
            };
        }

        private static void AddUserAffinity(
            HashSet<int> types,
            HashSet<int> animalIds,
            HashSet<int> categoryIds,
            Activity activity)
        {
            types.Add(activity.ActivityTypeId);
            if (activity.AnimalId.HasValue)
            {
                animalIds.Add(activity.AnimalId.Value);
            }

            if (activity.Animal != null)
            {
                categoryIds.Add(activity.Animal.CategoryId);
            }
        }

        private static RecommendationItemResponse BuildItem(
            Activity activity,
            ScoringContext context,
            float? mfPrediction)
        {
            context.ReviewStats.TryGetValue(activity.Id, out var stats);
            context.Popularity.TryGetValue(activity.Id, out var reservationCount);

            var avgRating = stats.Count > 0 ? stats.Avg : 0;
            var ratingForScore = stats.Count > 0 ? stats.Avg : context.GlobalMeanRating;
            var typeAffinity = context.UserTypes.Contains(activity.ActivityTypeId);
            var typeName = string.IsNullOrWhiteSpace(activity.ActivityType?.Name)
                ? "this type"
                : activity.ActivityType.Name;
            var animalAffinity = activity.AnimalId.HasValue && context.UserAnimalIds.Contains(activity.AnimalId.Value);
            var categoryAffinity = activity.Animal != null && context.UserCategoryIds.Contains(activity.Animal.CategoryId);
            var adoptionBoost = activity.AnimalId.HasValue && context.UserAdoptedAnimalIds.Contains(activity.AnimalId.Value);
            var popularityNorm = Math.Min(reservationCount / RecommendationWeights.PopularityCap, 1.0);

            var mfNorm = 0.0;
            if (mfPrediction.HasValue)
            {
                var clamped = Math.Clamp(mfPrediction.Value, 1f, 5f);
                mfNorm = clamped / 5.0;
            }

            var contributions = new List<(string Code, string Text, double Weight)>
            {
                (RecommendationWeights.Collaborative,
                    "Visitors with similar reviews also liked this",
                    RecommendationWeights.MatrixFactorization * mfNorm),
                (RecommendationWeights.HighRating,
                    $"Highly rated — {avgRating:0.0}/5 from {stats.Count} review{(stats.Count == 1 ? "" : "s")}",
                    RecommendationWeights.AvgRating * (ratingForScore / 5.0)),
                (RecommendationWeights.SameType,
                    $"You previously chose a {typeName} activity",
                    typeAffinity ? RecommendationWeights.TypeAffinity : 0),
                (RecommendationWeights.SameAnimal,
                    $"Features {activity.Animal?.Name}, which you've already engaged with",
                    animalAffinity ? RecommendationWeights.AnimalAffinity : 0),
                (RecommendationWeights.SameCategory,
                    "Same animal category as activities you've booked",
                    categoryAffinity ? RecommendationWeights.CategoryAffinity : 0),
                (RecommendationWeights.YouAdopted,
                    $"You adopted {activity.Animal?.Name}",
                    adoptionBoost ? RecommendationWeights.AdoptionBoost : 0),
                (RecommendationWeights.Popular,
                    "Popular among visitors",
                    RecommendationWeights.Popularity * popularityNorm)
            };

            var score =
                RecommendationWeights.MatrixFactorization * mfNorm +
                RecommendationWeights.AvgRating * (ratingForScore / 5.0) +
                (typeAffinity ? RecommendationWeights.TypeAffinity : 0) +
                (animalAffinity ? RecommendationWeights.AnimalAffinity : 0) +
                (categoryAffinity ? RecommendationWeights.CategoryAffinity : 0) +
                (adoptionBoost ? RecommendationWeights.AdoptionBoost : 0) +
                RecommendationWeights.Popularity * popularityNorm;

            var reasons = contributions
                .Where(c => c.Weight > 0)
                .Where(c => c.Code != RecommendationWeights.HighRating
                    || (stats.Count > 0 && avgRating >= RecommendationWeights.HighRatingThreshold))
                .Where(c => c.Code != RecommendationWeights.Collaborative || mfNorm > 0)
                .Where(c => c.Code != RecommendationWeights.Popular || reservationCount >= 1)
                .OrderByDescending(c => c.Weight)
                .Select(c => new RecommendationReasonResponse { Code = c.Code, Text = c.Text })
                .ToList();

            return new RecommendationItemResponse
            {
                ActivityId = activity.Id,
                Name = activity.Name,
                ActivityTypeId = activity.ActivityTypeId,
                ActivityTypeName = activity.ActivityType?.Name ?? string.Empty,
                AnimalId = activity.AnimalId,
                AnimalName = activity.Animal?.Name,
                ImageUrl = activity.ImageUrl,
                Price = activity.Price,
                Score = Math.Round(score, 4),
                AvgRating = stats.Count > 0 ? Math.Round(avgRating, 2) : 0,
                ReviewCount = stats.Count,
                Reasons = reasons
            };
        }

        private PredictionEngine<ActivityRatingRow, ActivityRatingPrediction>? CreatePredictionEngine(RecommendationModelState state)
        {
            if (state.Model == null || state.MlContext == null)
            {
                return null;
            }

            try
            {
                return state.MlContext.Model.CreatePredictionEngine<ActivityRatingRow, ActivityRatingPrediction>(state.Model);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create recommendation prediction engine.");
                return null;
            }
        }

        private float? PredictScore(PredictionEngine<ActivityRatingRow, ActivityRatingPrediction>? engine, int userId, int activityId)
        {
            if (engine == null)
            {
                return null;
            }

            try
            {
                var prediction = engine.Predict(new ActivityRatingRow
                {
                    UserId = userId,
                    ActivityId = activityId
                });

                if (float.IsNaN(prediction.Score) || float.IsInfinity(prediction.Score))
                {
                    return null;
                }

                return prediction.Score;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "MF prediction failed for user {UserId} activity {ActivityId}.", userId, activityId);
                return null;
            }
        }

        private static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return double.PositiveInfinity;
            }

            values.Sort();
            var mid = values.Count / 2;
            if (values.Count % 2 == 0)
            {
                return (values[mid - 1] + values[mid]) / 2.0;
            }

            return values[mid];
        }

        private sealed class ScoringContext
        {
            public required List<Activity> Candidates { get; init; }

            public required Dictionary<int, (double Avg, int Count)> ReviewStats { get; init; }

            public required Dictionary<int, int> Popularity { get; init; }

            public required HashSet<int> UserTypes { get; init; }

            public required HashSet<int> UserAnimalIds { get; init; }

            public required HashSet<int> UserCategoryIds { get; init; }

            public required HashSet<int> UserAdoptedAnimalIds { get; init; }

            public required double GlobalMeanRating { get; init; }
        }
    }
}
