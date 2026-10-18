using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class NewsService : BaseCRUDService<NewsItem, NewsResponse, NewsSearchObject, NewsInsertRequest, NewsUpdateRequest>, INewsService
    {
        public const string ImageRequiredMessage = "Image is required. Use a PNG or JPEG file of at most 512 KB.";
        public const string SignInMessage = "You must be signed in to add news.";
        private const int ExcerptLength = 180;

        private readonly IStoredImageService _images;
        private readonly IAuthenticatedUserAccessor _userAccessor;

        static NewsService()
        {
            TypeAdapterConfig<NewsItem, NewsResponse>.NewConfig()
                .Map(dest => dest.AuthorName, src => src.Author == null
                    ? string.Empty
                    : (src.Author.FirstName + " " + src.Author.LastName).Trim())
                .Map(dest => dest.PublishedAt, src => DateTime.SpecifyKind(src.PublishedAt, DateTimeKind.Utc))
                .Map(dest => dest.CreatedAt, src => DateTime.SpecifyKind(src.CreatedAt, DateTimeKind.Utc));
        }

        public NewsService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<NewsInsertRequest> insertValidator,
            IValidator<NewsUpdateRequest> updateValidator,
            IStoredImageService images,
            IAuthenticatedUserAccessor userAccessor,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _images = images;
            _userAccessor = userAccessor;
        }

        protected override string NotFoundMessage => "News item not found.";

        protected override IQueryable<NewsItem> ApplyFilters(IQueryable<NewsItem> query, NewsSearchObject? search)
        {
            if (search == null)
            {
                return query;
            }

            if (search.IsPublished.HasValue)
            {
                query = query.Where(item => item.IsPublished == search.IsPublished.Value);
            }

            if (!string.IsNullOrWhiteSpace(search.FTS))
            {
                var term = search.FTS.Trim();
                query = query.Where(item => item.Title.Contains(term) || item.Text.Contains(term));
            }

            return query;
        }

        protected override IQueryable<NewsItem> ApplyDefaultOrder(IQueryable<NewsItem> query)
        {
            return query.OrderByDescending(item => item.PublishedAt).ThenByDescending(item => item.Id);
        }

        protected override Task<IQueryable<NewsItem>> IncludeRelatedEntitiesAsync(NewsSearchObject? search, IQueryable<NewsItem> query)
        {
            IQueryable<NewsItem> withAuthor = query.Include(item => item.Author);
            return Task.FromResult(withAuthor);
        }

        public override async Task<NewsResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.NewsItems
                .AsNoTracking()
                .Include(item => item.Author)
                .FirstOrDefaultAsync(item => item.Id == id);

            if (entity == null)
            {
                ThrowNotFound(id);
            }

            return _mapper.Map<NewsResponse>(entity);
        }

        public async Task<PageResult<NewsListItemResponse>> GetPublishedAsync(NewsSearchObject? search = null)
        {
            search ??= new NewsSearchObject();
            search.IsPublished = true;
            search.SortBy = null;

            var page = await GetAllAsync(search);
            return new PageResult<NewsListItemResponse>
            {
                TotalCount = page.TotalCount,
                Items = page.Items.Select(ToListItem).ToList()
            };
        }

        public async Task<NewsResponse> GetPublishedByIdAsync(int id)
        {
            var item = await GetByIdAsync(id);
            if (!item.IsPublished)
            {
                ThrowNotFound(id);
            }

            return item;
        }

        public override async Task<NewsResponse> InsertAsync(NewsInsertRequest request)
        {
            Normalize(request);
            await RequireAuthorAsync();
            request.PublishedAt = AsUtc(request.PublishedAt);
            await ValidateInsertAsync(request);

            string? created = null;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.ImageUrl = created;
                var saved = await base.InsertAsync(request);
                created = null;
                return await GetByIdAsync(saved.Id);
            }
            catch
            {
                _images.Delete(created);
                throw;
            }
        }

        public override async Task<NewsResponse> UpdateAsync(int id, NewsUpdateRequest request)
        {
            Normalize(request);
            request.PublishedAt = AsUtc(request.PublishedAt);
            await ValidateUpdateAsync(request);

            var entity = await _dbContext.NewsItems.FirstOrDefaultAsync(item => item.Id == id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            var previousImage = entity.ImageUrl;
            string? created = null;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.ImageUrl = created ?? previousImage;
                request.ImageBase64 = null;
                if (string.IsNullOrWhiteSpace(request.ImageUrl))
                {
                    throw new ClientException(ImageRequiredMessage);
                }

                var saved = await base.UpdateAsync(id, request);
                if (created != null && !string.Equals(previousImage, created, StringComparison.Ordinal))
                {
                    _images.Delete(previousImage);
                }

                created = null;
                return await GetByIdAsync(saved.Id);
            }
            catch
            {
                _images.Delete(created);
                throw;
            }
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.NewsItems.FirstOrDefaultAsync(item => item.Id == id);
            if (entity == null)
            {
                ThrowNotFound(id);
            }

            var image = entity.ImageUrl;
            _dbContext.NewsItems.Remove(entity);
            await _dbContext.SaveChangesAsync();
            _images.Delete(image);
        }

        protected override NewsItem MapInsertRequestToEntity(NewsInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            var userId = _userAccessor.GetUserId();
            if (userId == null)
            {
                throw new ClientException(SignInMessage);
            }

            entity.AuthorUserId = userId.Value;
            entity.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
            entity.PublishedAt = AsUtc(request.PublishedAt);
            return entity;
        }

        private async Task ValidateInsertAsync(NewsInsertRequest request)
        {
            var result = await _insertValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task ValidateUpdateAsync(NewsUpdateRequest request)
        {
            var result = await _updateValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task RequireAuthorAsync()
        {
            var userId = _userAccessor.GetUserId();
            if (userId == null || !await _dbContext.Users.AnyAsync(user => user.Id == userId.Value))
            {
                throw new ClientException(SignInMessage);
            }
        }

        private static NewsListItemResponse ToListItem(NewsResponse item)
        {
            return new NewsListItemResponse
            {
                Id = item.Id,
                Title = item.Title,
                Excerpt = Excerpt(item.Text),
                ImageUrl = item.ImageUrl,
                PublishedAt = item.PublishedAt,
                AuthorName = item.AuthorName
            };
        }

        private static string Excerpt(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.Length <= ExcerptLength)
            {
                return trimmed;
            }

            return trimmed[..ExcerptLength].TrimEnd() + "...";
        }

        private static void Normalize(NewsInsertRequest request)
        {
            request.Title = request.Title?.Trim() ?? string.Empty;
            request.Text = request.Text?.Trim() ?? string.Empty;
        }

        private static void Normalize(NewsUpdateRequest request)
        {
            request.Title = request.Title?.Trim() ?? string.Empty;
            request.Text = request.Text?.Trim() ?? string.Empty;
        }

        private static DateTime AsUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
