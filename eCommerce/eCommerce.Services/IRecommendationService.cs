using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationResponse> GetRecommendationsAsync(int? limit);

        Task<RecommendationRetrainResponse> RetrainAsync();
    }
}
