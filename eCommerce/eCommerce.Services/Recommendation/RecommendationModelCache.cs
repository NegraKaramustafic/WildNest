using Microsoft.ML;

namespace eCommerce.Services.Recommendation
{
    public sealed class RecommendationModelState
    {
        public RecommendationModelState(
            ITransformer? model,
            MLContext? mlContext,
            DateTime? trainedAtUtc,
            int trainingRowCount,
            int qualifyingReservationCount,
            IReadOnlySet<int> userIds,
            IReadOnlySet<int> activityIds,
            DateTime? lastAttemptUtc,
            int countAtLastAttempt)
        {
            Model = model;
            MlContext = mlContext;
            TrainedAtUtc = trainedAtUtc;
            TrainingRowCount = trainingRowCount;
            QualifyingReservationCount = qualifyingReservationCount;
            UserIds = userIds;
            ActivityIds = activityIds;
            LastAttemptUtc = lastAttemptUtc;
            CountAtLastAttempt = countAtLastAttempt;
        }

        public ITransformer? Model { get; }

        public MLContext? MlContext { get; }

        public DateTime? TrainedAtUtc { get; }

        public int TrainingRowCount { get; }

        public int QualifyingReservationCount { get; }

        public IReadOnlySet<int> UserIds { get; }

        public IReadOnlySet<int> ActivityIds { get; }

        public DateTime? LastAttemptUtc { get; }

        public int CountAtLastAttempt { get; }
    }

    public sealed class RecommendationModelCache
    {
        private readonly object _sync = new();
        private ITransformer? _model;
        private MLContext? _mlContext;
        private DateTime? _trainedAtUtc;
        private int _trainingRowCount;
        private int _qualifyingReservationCount;
        private IReadOnlySet<int> _userIds = new HashSet<int>();
        private IReadOnlySet<int> _activityIds = new HashSet<int>();
        private DateTime? _lastAttemptUtc;
        private int _countAtLastAttempt;

        public RecommendationModelState Capture()
        {
            lock (_sync)
            {
                return new RecommendationModelState(
                    _model,
                    _mlContext,
                    _trainedAtUtc,
                    _trainingRowCount,
                    _qualifyingReservationCount,
                    _userIds,
                    _activityIds,
                    _lastAttemptUtc,
                    _countAtLastAttempt);
            }
        }

        public void RecordAttempt(int qualifyingReservationCount)
        {
            lock (_sync)
            {
                _lastAttemptUtc = DateTime.UtcNow;
                _countAtLastAttempt = qualifyingReservationCount;
            }
        }

        public void SetTrained(
            ITransformer? model,
            MLContext? mlContext,
            int trainingRowCount,
            int qualifyingReservationCount,
            IReadOnlySet<int> userIds,
            IReadOnlySet<int> activityIds)
        {
            lock (_sync)
            {
                _model = model;
                _mlContext = mlContext;
                _trainingRowCount = trainingRowCount;
                _qualifyingReservationCount = qualifyingReservationCount;
                _userIds = userIds;
                _activityIds = activityIds;
                _trainedAtUtc = model == null ? _trainedAtUtc : DateTime.UtcNow;
                if (model != null)
                {
                    _lastAttemptUtc = _trainedAtUtc;
                    _countAtLastAttempt = qualifyingReservationCount;
                }
            }
        }
    }
}
