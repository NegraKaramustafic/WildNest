using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;

namespace eCommerce.Services
{
    public interface INewsService : IBaseCRUDService<NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest>
    {
        Task<PageResult<NewsListItemResponse>> GetPublishedAsync(NewsSearchObject? search = null);

        Task<NewsResponse> GetPublishedByIdAsync(int id);
    }
}
