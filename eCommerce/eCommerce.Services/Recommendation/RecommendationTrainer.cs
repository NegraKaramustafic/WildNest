using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Trainers;

namespace eCommerce.Services.Recommendation
{
    public sealed class RecommendationTrainResult
    {
        public DateTime TrainedAt { get; init; }

        public int TrainingRowCount { get; init; }

        public bool ModelReady { get; init; }
    }

    /// <summary>
    /// Trains MatrixFactorization on confirmed and completed activity reservations.
    /// Each distinct visitor-activity pair is one positive interaction. Suggestion
    /// requests never train. A background job calls this, and an administrator can retrain explicitly.
    /// </summary>
    public sealed class RecommendationTrainer
    {
        private readonly RecommendationModelCache _cache;
        private readonly ILogger<RecommendationTrainer> _logger;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public RecommendationTrainer(RecommendationModelCache cache, ILogger<RecommendationTrainer> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<bool> IsDueAsync(
            ECommerceDbContext db,
            RecommendationTrainingSettings settings,
            CancellationToken cancellationToken)
        {
            var state = _cache.Capture();
            if (state.LastAttemptUtc == null)
            {
                return true;
            }

            if (DateTime.UtcNow - state.LastAttemptUtc.Value >= TimeSpan.FromMinutes(settings.IntervalMinutes))
            {
                return true;
            }

            var count = await CountQualifyingAsync(db, cancellationToken);
            return count - state.CountAtLastAttempt >= settings.NewReservationsBeforeRetrain;
        }

        public async Task<RecommendationTrainResult> TrainAsync(
            ECommerceDbContext db,
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                return await TrainCoreAsync(db, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<RecommendationTrainResult> TrainCoreAsync(
            ECommerceDbContext db,
            CancellationToken cancellationToken)
        {
            var qualifyingCount = await CountQualifyingAsync(db, cancellationToken);
            _cache.RecordAttempt(qualifyingCount);

            var pairs = await db.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Status == ReservationStatus.Confirmed
                    || reservation.Status == ReservationStatus.Completed)
                .Select(reservation => new
                {
                    reservation.UserId,
                    ActivityId = reservation.Schedule.ActivityId
                })
                .Distinct()
                .ToListAsync(cancellationToken);

            var rows = pairs
                .Select(pair => new ActivityRatingRow
                {
                    UserId = pair.UserId,
                    ActivityId = pair.ActivityId,
                    Label = RecommendationWeights.PositiveInteractionLabel
                })
                .ToList();

            var userIds = pairs.Select(pair => pair.UserId).ToHashSet();
            var activityIds = pairs.Select(pair => pair.ActivityId).ToHashSet();
            if (rows.Count < RecommendationWeights.MinimumTrainingPairs || userIds.Count < 2 || activityIds.Count < 2)
            {
                _logger.LogInformation(
                    "Skipping MatrixFactorization training; {RowCount} reservation pairs from {Users} visitors and {Activities} activities (minimum {Min} pairs, 2 visitors, and 2 activities). Suggestions will use popularity.",
                    rows.Count,
                    userIds.Count,
                    activityIds.Count,
                    RecommendationWeights.MinimumTrainingPairs);
                _cache.SetTrained(
                    null,
                    null,
                    rows.Count,
                    qualifyingCount,
                    new HashSet<int>(),
                    new HashSet<int>());
                return Result(rows.Count, modelReady: false);
            }

            try
            {
                var mlContext = new MLContext(seed: 1);
                var data = mlContext.Data.LoadFromEnumerable(rows);
                var rank = Math.Max(1, Math.Min(2, Math.Min(userIds.Count, activityIds.Count) - 1));
                var model = Fit(mlContext, data, rank, learningRate: 0.02, iterations: 60);
                if (!ScoresAreFinite(mlContext, model, data))
                {
                    _logger.LogWarning(
                        "MatrixFactorization scores were not finite. Retrying with a smaller step.");
                    model = Fit(mlContext, data, rank: 1, learningRate: 0.005, iterations: 40);
                }

                if (!ScoresAreFinite(mlContext, model, data))
                {
                    _logger.LogError(
                        "MatrixFactorization did not produce finite scores. Suggestions will use popularity.");
                    _cache.SetTrained(
                        null,
                        null,
                        rows.Count,
                        qualifyingCount,
                        new HashSet<int>(),
                        new HashSet<int>());
                    return Result(rows.Count, modelReady: false);
                }

                _cache.SetTrained(model, mlContext, rows.Count, qualifyingCount, userIds, activityIds);
                _logger.LogInformation(
                    "Trained recommendation model on {RowCount} confirmed or completed reservation pairs.",
                    rows.Count);
                return Result(rows.Count, modelReady: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "MatrixFactorization training failed. The previous model is kept when one exists; otherwise suggestions use popularity.");
                var previous = _cache.Capture();
                return new RecommendationTrainResult
                {
                    TrainedAt = previous.TrainedAtUtc ?? previous.LastAttemptUtc ?? DateTime.UtcNow,
                    TrainingRowCount = previous.Model == null ? rows.Count : previous.TrainingRowCount,
                    ModelReady = previous.Model != null
                };
            }
        }

        private static ITransformer Fit(MLContext mlContext, IDataView data, int rank, double learningRate, int iterations)
        {
            var options = new MatrixFactorizationTrainer.Options
            {
                MatrixColumnIndexColumnName = "UserIdEncoded",
                MatrixRowIndexColumnName = "ActivityIdEncoded",
                LabelColumnName = nameof(ActivityRatingRow.Label),
                NumberOfIterations = iterations,
                ApproximationRank = rank,
                LearningRate = learningRate,
                Lambda = 0.05,
                Quiet = true
            };

            var pipeline = mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "UserIdEncoded",
                    inputColumnName: nameof(ActivityRatingRow.UserId))
                .Append(mlContext.Transforms.Conversion.MapValueToKey(
                    outputColumnName: "ActivityIdEncoded",
                    inputColumnName: nameof(ActivityRatingRow.ActivityId)))
                .Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));
            return pipeline.Fit(data);
        }

        private static bool ScoresAreFinite(MLContext mlContext, ITransformer model, IDataView data)
        {
            foreach (var row in mlContext.Data.CreateEnumerable<ActivityRatingPrediction>(
                model.Transform(data),
                reuseRowObject: false))
            {
                if (float.IsNaN(row.Score) || float.IsInfinity(row.Score))
                {
                    return false;
                }
            }

            return true;
        }

        private static async Task<int> CountQualifyingAsync(ECommerceDbContext db, CancellationToken cancellationToken)
        {
            return await db.Reservations.CountAsync(
                reservation =>
                    reservation.Status == ReservationStatus.Confirmed
                    || reservation.Status == ReservationStatus.Completed,
                cancellationToken);
        }

        private RecommendationTrainResult Result(int trainingRowCount, bool modelReady)
        {
            var state = _cache.Capture();
            return new RecommendationTrainResult
            {
                TrainedAt = state.TrainedAtUtc ?? state.LastAttemptUtc ?? DateTime.UtcNow,
                TrainingRowCount = trainingRowCount,
                ModelReady = modelReady
            };
        }
    }
}
