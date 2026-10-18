using eCommerce.Model.Responses;

namespace eCommerce.Services
{
    public interface IDashboardService
    {
        Task<DashboardResponse> GetAsync();
    }
}
