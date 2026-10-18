using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly RecommendationModelCache _modelCache;
        private readonly RecommendationTrainer _trainer;
        private readonly ZooClock _zooClock;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            ECommerceDbContext dbContext,
            IAuthenticatedUserAccessor userAccessor,
            RecommendationModelCache modelCache,
            RecommendationTrainer trainer,
            ZooClock zooClock,
            ILogger<RecommendationService> logger)
        {
            _dbContext = dbContext;
            _userAccessor = userAccessor;
            _modelCache = modelCache;
            _trainer = trainer;
            _zooClock = zooClock;
            _logger = logger;
        }

        public async Task<RecommendationResponse> GetRecommendationsAsync(int? limit)
        {
            var userId = _userAccessor.GetUserId()
                ?? throw new UnauthorizedAccessException("User id claim is missing.");
            var take = ResolveLimit(limit);
            var state = _modelCache.Capture();
            var context = await LoadCandidatesAsync(userId);
            var history = await CountDistinctHistoryAsync(userId);
            var personalized = state.Model != null
                && history >= RecommendationWeights.MinimumDistinctReservedActivities
                && state.UserIds.Contains(userId);

            List<RecommendationItemResponse> items;
            if (!personalized)
            {
                items = RankByPopularity(context, take);
            }
            else
            {
                items = RankByModel(state, context, userId, take);
                if (items.Count == 0)
                {
                    items = RankByPopularity(context, take);
                }
            }

            return new RecommendationResponse
            {
                GeneratedAt = DateTime.UtcNow,
                ModelTrainedAt = state.Model == null ? null : state.TrainedAtUtc,
                TrainingRowCount = state.Model == null ? 0 : state.TrainingRowCount,
                Items = items
            };
        }

        public async Task<RecommendationRetrainResponse> RetrainAsync()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                throw new UnauthorizedAccessException("Only administrators can retrain the recommendation model.");
            }

            var trained = await _trainer.TrainAsync(_dbContext);
            return new RecommendationRetrainResponse
            {
                TrainedAt = trained.TrainedAt,
                TrainingRowCount = trained.TrainingRowCount
            };
        }

        private async Task<int> CountDistinctHistoryAsync(int userId)
        {
            return await _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.UserId == userId
                    && (reservation.Status == ReservationStatus.Confirmed
                        || reservation.Status == ReservationStatus.Completed))
                .Select(reservation => reservation.Schedule.ActivityId)
                .Distinct()
                .CountAsync();
        }

        private async Task<ScoringContext> LoadCandidatesAsync(int userId)
        {
            var localNow = _zooClock.ToLocal(DateTime.UtcNow);
            var today = DateOnly.FromDateTime(localNow);
            var time = TimeOnly.FromDateTime(localNow);

            var reservedIds = await _dbContext.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.UserId == userId
                    && reservation.Status != ReservationStatus.Cancelled)
                .Select(reservation => reservation.Schedule.ActivityId)
                .Distinct()
                .ToListAsync();

            var query = _dbContext.Activities
                .AsNoTracking()
                .Include(activity => activity.Animal)
                .Include(activity => activity.ActivityType)
                .Where(activity => activity.Status == ActivityStatus.Active)
                .Where(activity => activity.Schedules.Any(schedule =>
                    schedule.Status == ScheduleStatus.Active
                    && (schedule.Date > today
                        || (schedule.Date == today && schedule.StartTime >= time))));

            if (reservedIds.Count > 0)
            {
                query = query.Where(activity => !reservedIds.Contains(activity.Id));
            }

            var candidates = await query.ToListAsync();
            var candidateIds = candidates.Select(activity => activity.Id).ToList();
            var popularity = new Dictionary<int, int>();
            var reviewStats = new Dictionary<int, (double Avg, int Count)>();
            if (candidateIds.Count > 0)
            {
                var popularityRows = await _dbContext.Reservations
                    .AsNoTracking()
                    .Where(reservation =>
                        (reservation.Status == ReservationStatus.Confirmed
                            || reservation.Status == ReservationStatus.Completed)
                        && candidateIds.Contains(reservation.Schedule.ActivityId))
                    .GroupBy(reservation => reservation.Schedule.ActivityId)
                    .Select(group => new
                    {
                        ActivityId = group.Key,
                        Count = group.Count()
                    })
                    .ToListAsync();
                popularity = popularityRows.ToDictionary(row => row.ActivityId, row => row.Count);

                var reviewRows = await _dbContext.Reviews
                    .AsNoTracking()
                    .Where(review => candidateIds.Contains(review.ActivityId))
                    .GroupBy(review => review.ActivityId)
                    .Select(group => new
                    {
                        ActivityId = group.Key,
                        Avg = group.Average(review => (double)review.Rating),
                        Count = group.Count()
                    })
                    .ToListAsync();
                reviewStats = reviewRows.ToDictionary(
                    row => row.ActivityId,
                    row => (row.Avg, row.Count));
            }

            return new ScoringContext(candidates, popularity, reviewStats);
        }

        private List<RecommendationItemResponse> RankByModel(
            RecommendationModelState state,
            ScoringContext context,
            int userId,
            int take)
        {
            if (state.Model == null || state.MlContext == null)
            {
                return new List<RecommendationItemResponse>();
            }

            var activities = context.Candidates
                .Where(activity => state.ActivityIds.Contains(activity.Id))
                .ToList();
            if (activities.Count == 0)
            {
                return new List<RecommendationItemResponse>();
            }

            var rows = activities
                .Select(activity => new ActivityRatingRow
                {
                    UserId = userId,
                    ActivityId = activity.Id
                })
                .ToList();

            float[] scores;
            try
            {
                var data = state.MlContext.Data.LoadFromEnumerable(rows);
                var transformed = state.Model.Transform(data);
                scores = state.MlContext.Data
                    .CreateEnumerable<ActivityRatingPrediction>(transformed, reuseRowObject: false)
                    .Select(prediction => prediction.Score)
                    .ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Matrix factorization scoring failed for user {UserId}.", userId);
                return new List<RecommendationItemResponse>();
            }

            if (scores.Length != activities.Count)
            {
                _logger.LogWarning(
                    "Matrix factorization returned {ScoreCount} scores for {ActivityCount} activities.",
                    scores.Length,
                    activities.Count);
                return new List<RecommendationItemResponse>();
            }

            var scored = new List<(Activity Activity, float Score)>();
            for (var index = 0; index < activities.Count; index++)
            {
                var score = scores[index];
                if (float.IsNaN(score) || float.IsInfinity(score))
                {
                    continue;
                }

                scored.Add((activities[index], score));
            }

            if (scored.Count < activities.Count)
            {
                _logger.LogWarning(
                    "Matrix factorization returned a non-finite score for {Skipped} of {ActivityCount} activities.",
                    activities.Count - scored.Count,
                    activities.Count);
            }

            return scored
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Activity.Name)
                .Take(take)
                .Select(item => BuildItem(
                    item.Activity,
                    context,
                    Math.Round(item.Score, 4),
                    RecommendationWeights.SimilarReservations,
                    RecommendationWeights.SimilarReservationsText))
                .ToList();
        }

        private static List<RecommendationItemResponse> RankByPopularity(ScoringContext context, int take)
        {
            return context.Candidates
                .Select(activity =>
                {
                    context.Popularity.TryGetValue(activity.Id, out var count);
                    return (Activity: activity, Count: count);
                })
                .Where(item => item.Count > 0)
                .OrderByDescending(item => item.Count)
                .ThenBy(item => item.Activity.Name)
                .Take(take)
                .Select(item => BuildItem(
                    item.Activity,
                    context,
                    item.Count,
                    RecommendationWeights.Popular,
                    RecommendationWeights.PopularText))
                .ToList();
        }

        private static RecommendationItemResponse BuildItem(
            Activity activity,
            ScoringContext context,
            double score,
            string reasonCode,
            string reasonText)
        {
            context.ReviewStats.TryGetValue(activity.Id, out var stats);
            var avgRating = stats.Count > 0 ? stats.Avg : 0;
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
                Score = score,
                AvgRating = stats.Count > 0 ? Math.Round(avgRating, 2) : 0,
                ReviewCount = stats.Count,
                Reasons = new List<RecommendationReasonResponse>
                {
                    new()
                    {
                        Code = reasonCode,
                        Text = reasonText
                    }
                }
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

        private sealed class ScoringContext
        {
            public ScoringContext(
                List<Activity> candidates,
                Dictionary<int, int> popularity,
                Dictionary<int, (double Avg, int Count)> reviewStats)
            {
                Candidates = candidates;
                Popularity = popularity;
                ReviewStats = reviewStats;
            }

            public List<Activity> Candidates { get; }

            public Dictionary<int, int> Popularity { get; }

            public Dictionary<int, (double Avg, int Count)> ReviewStats { get; }
        }
    }
}
