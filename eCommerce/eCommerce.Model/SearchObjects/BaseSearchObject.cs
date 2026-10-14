
using eCommerce.Model;

namespace eCommerce.Model.SearchObjects
{
    public class BaseSearchObject
    {
        public int? Page { get; set; } = Pagination.DefaultPage;

        public int? PageSize { get; set; } = Pagination.DefaultPageSize;

        public bool? IncludeTotalCount { get; set; } = false;
        public string? SortBy { get; set; } 
    }
}
