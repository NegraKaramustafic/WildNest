using System.Linq;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class BadgeService : BaseCRUDService<Badge, BadgeResponse, BadgeSearchObject, BadgeInsertRequest, BadgeUpdateRequest>, IBadgeService
    {
        private readonly IStoredImageService _images;

        public BadgeService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<BadgeInsertRequest> insertValidator,
            IValidator<BadgeUpdateRequest> updateValidator,
            IStoredImageService images,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _images = images;
        }

        protected override string NotFoundMessage => "Badge not found.";

        protected override IQueryable<Badge> ApplyFilters(IQueryable<Badge> query, BadgeSearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name.Trim();
                    query = query.Where(b => b.Name.Contains(name));
                }

                if (search.Criteria.HasValue)
                {
                    query = query.Where(b => b.Criteria == search.Criteria.Value);
                }
            }

            return query;
        }

        protected override IQueryable<Badge> ApplyDefaultOrder(IQueryable<Badge> query)
        {
            return query.OrderBy(b => b.Name).ThenBy(b => b.Id);
        }

        public override async Task<PageResult<BadgeResponse>> GetAllAsync(BadgeSearchObject? search = null)
        {
            var page = await base.GetAllAsync(search);
            await AttachHolderCountsAsync(page.Items);
            return page;
        }

        public override async Task<BadgeResponse> GetByIdAsync(int id)
        {
            var item = await base.GetByIdAsync(id);
            await AttachHolderCountsAsync(new List<BadgeResponse> { item });
            return item;
        }

        public override async Task<BadgeResponse> InsertAsync(BadgeInsertRequest request)
        {
            Normalize(request);
            await ValidateInsertAsync(request);
            await EnsureUniqueNameAsync(request.Name, null);

            string? created = null;
            int createdId;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.IconUrl = ImageUpload.ResolveUrl(null, request.IconUrl, created);
                request.ImageBase64 = null;
                var createdBadge = await base.InsertAsync(request);
                createdId = createdBadge.Id;
            }
            catch (DbUpdateException ex) when (IsUniqueNameViolation(ex))
            {
                _images.Delete(created);
                throw DuplicateName(request.Name);
            }
            catch
            {
                _images.Delete(created);
                throw;
            }

            return await GetByIdAsync(createdId);
        }

        public override async Task<BadgeResponse> UpdateAsync(int id, BadgeUpdateRequest request)
        {
            Normalize(request);
            await ValidateUpdateAsync(request);
            await EnsureUniqueNameAsync(request.Name, id);

            var entity = await _dbContext.Badges.FirstOrDefaultAsync(badge => badge.Id == id);
            if (entity == null)
            {
                ThrowNotFound("Badge", id, "Badge not found.");
            }

            var previous = entity.IconUrl;
            string? created = null;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.IconUrl = ImageUpload.ResolveUrl(previous, request.IconUrl, created);
                request.ImageBase64 = null;
                MapUpdateRequestToEntity(request, entity);
                entity.IconUrl = string.IsNullOrWhiteSpace(entity.IconUrl) ? null : entity.IconUrl.Trim();
                await _dbContext.SaveChangesAsync();
                if (!string.Equals(previous, entity.IconUrl, StringComparison.Ordinal))
                {
                    _images.Delete(previous);
                }
            }
            catch (DbUpdateException ex) when (IsUniqueNameViolation(ex))
            {
                _images.Delete(created);
                throw DuplicateName(request.Name);
            }
            catch
            {
                _images.Delete(created);
                throw;
            }

            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var exists = await _dbContext.Badges.AnyAsync(badge => badge.Id == id);
            if (!exists)
            {
                ThrowNotFound("Badge", id, "Badge not found.");
            }

            if (await _dbContext.UserBadges.AnyAsync(holder => holder.BadgeId == id))
            {
                throw new ClientException("This badge has been awarded to users and cannot be deleted.");
            }

            var icon = await _dbContext.Badges.Where(badge => badge.Id == id).Select(badge => badge.IconUrl).FirstOrDefaultAsync();
            await base.DeleteAsync(id);
            _images.Delete(icon);
        }

        private async Task AttachHolderCountsAsync(IReadOnlyList<BadgeResponse> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var ids = items.Select(item => item.Id).ToList();
            var counts = await _dbContext.UserBadges
                .Where(holder => ids.Contains(holder.BadgeId))
                .GroupBy(holder => holder.BadgeId)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToListAsync();

            var map = counts.ToDictionary(item => item.Id, item => item.Count);
            foreach (var item in items)
            {
                item.HolderCount = map.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        private async Task EnsureUniqueNameAsync(string name, int? excludeId)
        {
            var normalized = name.Trim().ToLower();
            var exists = await _dbContext.Badges.AnyAsync(badge =>
                badge.Name.ToLower() == normalized &&
                (excludeId == null || badge.Id != excludeId.Value));

            if (exists)
            {
                throw DuplicateName(name);
            }
        }

        private async Task ValidateInsertAsync(BadgeInsertRequest request)
        {
            var result = await _insertValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task ValidateUpdateAsync(BadgeUpdateRequest request)
        {
            var result = await _updateValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private static void Normalize(BadgeInsertRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            request.IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? request.IconUrl : request.IconUrl.Trim();
        }

        private static void Normalize(BadgeUpdateRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            request.IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? request.IconUrl : request.IconUrl.Trim();
        }

        private static ValidationException DuplicateName(string name)
        {
            return new ValidationException(new[]
            {
                new ValidationFailure(nameof(BadgeInsertRequest.Name), $"A badge named '{name.Trim()}' already exists.")
            });
        }

        private static bool IsUniqueNameViolation(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("IX_Badges_Name", StringComparison.OrdinalIgnoreCase)
                || message.Contains("Badges.Name", StringComparison.OrdinalIgnoreCase);
        }
    }
}
