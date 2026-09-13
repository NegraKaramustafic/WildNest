using Microsoft.ML;

namespace eCommerce.Services.Recommendation
{
    public sealed class RecommendationModelState
    {
        public RecommendationModelState(ITransformer? model, MLContext? mlContext, DateTime? trainedAtUtc, int trainingRowCount)
        {
            Model = model;
            MlContext = mlContext;
            TrainedAtUtc = trainedAtUtc;
            TrainingRowCount = trainingRowCount;
        }

        public ITransformer? Model { get; }

        public MLContext? MlContext { get; }

        public DateTime? TrainedAtUtc { get; }

        public int TrainingRowCount { get; }
    }

    public sealed class RecommendationModelCache
    {
        private readonly object _sync = new();
        private ITransformer? _model;
        private MLContext? _mlContext;
        private DateTime? _trainedAtUtc;
        private int _trainingRowCount;
        private bool _isDirty = true;

        public void MarkDirty()
        {
            lock (_sync)
            {
                _isDirty = true;
            }
        }

        public bool NeedsTraining
        {
            get
            {
                lock (_sync)
                {
                    return _isDirty;
                }
            }
        }

        public RecommendationModelState Capture()
        {
            lock (_sync)
            {
                return new RecommendationModelState(_model, _mlContext, _trainedAtUtc, _trainingRowCount);
            }
        }

        public void SetTrained(ITransformer? model, MLContext? mlContext, int trainingRowCount)
        {
            lock (_sync)
            {
                _model = model;
                _mlContext = mlContext;
                _trainingRowCount = trainingRowCount;
                _trainedAtUtc = DateTime.UtcNow;
                _isDirty = false;
            }
        }
    }
}
