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
    public class ZooPartnerService : BaseCRUDService<ZooPartner, ZooPartnerResponse, ZooPartnerSearchObject, ZooPartnerInsertRequest, ZooPartnerUpdateRequest>, IZooPartnerService
    {
        private readonly IStoredImageService _images;

        public ZooPartnerService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ZooPartnerInsertRequest> insertValidator,
            IValidator<ZooPartnerUpdateRequest> updateValidator,
            IStoredImageService images,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _images = images;
        }

        protected override string NotFoundMessage => "Zoo partner not found.";

        protected override ZooPartner MapInsertRequestToEntity(ZooPartnerInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.CurrentAmount = 0;
            return entity;
        }

        protected override IQueryable<ZooPartner> ApplyFilters(IQueryable<ZooPartner> query, ZooPartnerSearchObject? search)
        {
            if (search != null)
            {
                if (search.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == search.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name.Trim();
                    query = query.Where(p => p.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(search.FTS))
                {
                    var term = search.FTS.Trim();
                    query = query.Where(partner =>
                        partner.Name.Contains(term)
                        || partner.Location.Contains(term)
                        || (partner.Description != null && partner.Description.Contains(term)));
                }
            }

            return query;
        }

        protected override IQueryable<ZooPartner> ApplyDefaultOrder(IQueryable<ZooPartner> query)
        {
            return query.OrderBy(p => p.Name).ThenBy(p => p.Id);
        }

        public override async Task<PageResult<ZooPartnerResponse>> GetAllAsync(ZooPartnerSearchObject? search = null)
        {
            var page = await base.GetAllAsync(search);
            await AttachDonationCountsAsync(page.Items);
            return page;
        }

        public override async Task<ZooPartnerResponse> GetByIdAsync(int id)
        {
            var item = await base.GetByIdAsync(id);
            await AttachDonationCountsAsync(new List<ZooPartnerResponse> { item });
            return item;
        }

        public override async Task<ZooPartnerResponse> InsertAsync(ZooPartnerInsertRequest request)
        {
            Normalize(request);
            await ValidateInsertAsync(request);
            await EnsureUniqueNameAsync(request.Name, null);

            string? created = null;
            int createdId;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.ImageUrl = ImageUpload.ResolveUrl(null, request.ImageUrl, created);
                request.ImageBase64 = null;
                var createdPartner = await base.InsertAsync(request);
                createdId = createdPartner.Id;
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

        public override async Task<ZooPartnerResponse> UpdateAsync(int id, ZooPartnerUpdateRequest request)
        {
            Normalize(request);
            await ValidateUpdateAsync(request);
            await EnsureUniqueNameAsync(request.Name, id);

            var entity = await _dbContext.ZooPartners.FirstOrDefaultAsync(partner => partner.Id == id);
            if (entity == null)
            {
                ThrowNotFound("ZooPartner", id, "Zoo partner not found.");
            }

            var previousImage = entity.ImageUrl;
            var currentAmount = entity.CurrentAmount;
            string? created = null;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.ImageUrl = ImageUpload.ResolveUrl(previousImage, request.ImageUrl, created);
                request.ImageBase64 = null;
                MapUpdateRequestToEntity(request, entity);
                entity.CurrentAmount = currentAmount;
                entity.ImageUrl = string.IsNullOrWhiteSpace(entity.ImageUrl) ? null : entity.ImageUrl.Trim();
                await _dbContext.SaveChangesAsync();
                if (!string.Equals(previousImage, entity.ImageUrl, StringComparison.Ordinal))
                {
                    _images.Delete(previousImage);
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
            var entity = await _dbContext.ZooPartners.FirstOrDefaultAsync(partner => partner.Id == id);
            if (entity == null)
            {
                ThrowNotFound("ZooPartner", id, "Zoo partner not found.");
            }

            if (await _dbContext.Donations.AnyAsync(donation => donation.ZooPartnerId == id))
            {
                throw new ClientException("This zoo partner has donations and cannot be deleted. Deactivate it instead.");
            }

            var image = entity.ImageUrl;
            await base.DeleteAsync(id);
            _images.Delete(image);
        }

        private async Task AttachDonationCountsAsync(IReadOnlyList<ZooPartnerResponse> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            var ids = items.Select(item => item.Id).ToList();
            var counts = await _dbContext.Donations
                .Where(donation => ids.Contains(donation.ZooPartnerId))
                .GroupBy(donation => donation.ZooPartnerId)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToListAsync();

            var map = counts.ToDictionary(item => item.Id, item => item.Count);
            foreach (var item in items)
            {
                item.DonationCount = map.TryGetValue(item.Id, out var count) ? count : 0;
            }
        }

        private async Task EnsureUniqueNameAsync(string name, int? excludeId)
        {
            var normalized = name.Trim().ToLower();
            var exists = await _dbContext.ZooPartners.AnyAsync(partner =>
                partner.Name.ToLower() == normalized &&
                (excludeId == null || partner.Id != excludeId.Value));

            if (exists)
            {
                throw DuplicateName(name);
            }
        }

        private async Task ValidateInsertAsync(ZooPartnerInsertRequest request)
        {
            var result = await _insertValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task ValidateUpdateAsync(ZooPartnerUpdateRequest request)
        {
            var result = await _updateValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private static void Normalize(ZooPartnerInsertRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Location = request.Location?.Trim() ?? string.Empty;
            request.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            request.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl : request.ImageUrl.Trim();
        }

        private static void Normalize(ZooPartnerUpdateRequest request)
        {
            request.Name = request.Name?.Trim() ?? string.Empty;
            request.Location = request.Location?.Trim() ?? string.Empty;
            request.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            request.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? request.ImageUrl : request.ImageUrl.Trim();
        }

        private static ValidationException DuplicateName(string name)
        {
            return new ValidationException(new[]
            {
                new ValidationFailure(nameof(ZooPartnerInsertRequest.Name), $"A zoo partner named '{name.Trim()}' already exists.")
            });
        }

        private static bool IsUniqueNameViolation(DbUpdateException exception)
        {
            var message = exception.InnerException?.Message ?? exception.Message;
            return message.Contains("IX_ZooPartners_Name", StringComparison.OrdinalIgnoreCase)
                || message.Contains("ZooPartners.Name", StringComparison.OrdinalIgnoreCase);
        }
    }
}
