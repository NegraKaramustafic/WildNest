using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class AnimalService : BaseCRUDService<Animal, AnimalResponse, AnimalSearchObject, AnimalInsertRequest, AnimalUpdateRequest>, IAnimalService
    {
        private readonly ZooClock _zooClock;
        private readonly IStoredImageService _images;

        public AnimalService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AnimalInsertRequest> insertValidator,
            IValidator<AnimalUpdateRequest> updateValidator,
            ZooClock zooClock,
            IStoredImageService images,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _zooClock = zooClock;
            _images = images;
        }

        protected override string NotFoundMessage => "Animal not found.";

        protected override async Task<IQueryable<Animal>> IncludeRelatedEntitiesAsync(AnimalSearchObject? search, IQueryable<Animal> query = null!)
        {
            query = query.Include(a => a.Category);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<AnimalResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Animal>()
                .AsNoTracking()
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null)
            {
                ThrowNotFound("Animal", id, "Animal not found.");
            }

            var response = _mapper.Map<AnimalResponse>(entity);
            await AttachActiveAdoptionCountsAsync(new[] { response });
            return response;
        }

        public override async Task<PageResult<AnimalResponse>> GetAllAsync(AnimalSearchObject? search = null)
        {
            var page = await base.GetAllAsync(search);
            await AttachActiveAdoptionCountsAsync(page.Items);
            return page;
        }

        private async Task AttachActiveAdoptionCountsAsync(IReadOnlyCollection<AnimalResponse> animals)
        {
            if (animals.Count == 0)
            {
                return;
            }

            var ids = animals.Select(animal => animal.Id).ToList();
            var today = _zooClock.Today;
            var counts = await _dbContext.Adoptions
                .AsNoTracking()
                .Where(adoption => ids.Contains(adoption.AnimalId))
                .Where(AdoptionActivity.CurrentlyActive(today))
                .GroupBy(adoption => adoption.AnimalId)
                .Select(group => new { AnimalId = group.Key, Count = group.Count() })
                .ToListAsync();

            var byAnimal = counts.ToDictionary(row => row.AnimalId, row => row.Count);
            foreach (var animal in animals)
            {
                animal.ActiveAdoptionCount = byAnimal.TryGetValue(animal.Id, out var count) ? count : 0;
            }
        }

        public override async Task<AnimalResponse> InsertAsync(AnimalInsertRequest request)
        {
            await EnsureCategoryExistsAsync(request.CategoryId);
            await ValidateInsertAsync(request);

            string? created = null;
            int createdId;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                request.ImageUrl = ImageUpload.ResolveUploaded(null, request.ImageUrl, created);
                request.ImageBase64 = null;
                var entity = MapInsertRequestToEntity(request);
                entity.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
                entity.CreatedAt = DateTime.UtcNow;
                _dbContext.Animals.Add(entity);
                await _dbContext.SaveChangesAsync();
                createdId = entity.Id;
                created = null;
            }
            catch
            {
                _images.Delete(created);
                throw;
            }

            return await GetByIdAsync(createdId);
        }

        public override async Task<AnimalResponse> UpdateAsync(int id, AnimalUpdateRequest request)
        {
            await EnsureCategoryExistsAsync(request.CategoryId);
            await ValidateUpdateAsync(request);

            var entity = await _dbContext.Animals.FirstOrDefaultAsync(animal => animal.Id == id);
            if (entity == null)
            {
                ThrowNotFound("Animal", id, "Animal not found.");
            }

            var previousImage = entity.ImageUrl;
            string? created = null;
            try
            {
                created = ImageUpload.StoreIfPresent(_images, request.ImageBase64, request.ImageContentType);
                var imageUrl = ImageUpload.ResolveUploaded(previousImage, request.ImageUrl, created);
                request.ImageUrl = imageUrl;
                request.ImageBase64 = null;
                MapUpdateRequestToEntity(request, entity);
                entity.ImageUrl = imageUrl;
                await _dbContext.SaveChangesAsync();
                created = null;
                if (!string.Equals(previousImage, entity.ImageUrl, StringComparison.Ordinal))
                {
                    _images.Delete(previousImage);
                }
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
            var entity = await _dbContext.Animals.FirstOrDefaultAsync(animal => animal.Id == id);
            if (entity == null)
            {
                ThrowNotFound("Animal", id, "Animal not found.");
            }

            var activityCount = await _dbContext.Activities.CountAsync(activity => activity.AnimalId == id);
            var adoptionCount = await _dbContext.Adoptions.CountAsync(adoption => adoption.AnimalId == id);
            if (activityCount > 0 || adoptionCount > 0)
            {
                throw new ClientException(InUseMessage(activityCount, adoptionCount));
            }

            var image = entity.ImageUrl;
            await base.DeleteAsync(id);
            _images.Delete(image);
        }

        private static string InUseMessage(int activityCount, int adoptionCount)
        {
            var parts = new List<string>();
            if (activityCount > 0)
            {
                parts.Add(CountLabel(activityCount, "activity", "activities"));
            }

            if (adoptionCount > 0)
            {
                parts.Add(CountLabel(adoptionCount, "adoption", "adoptions"));
            }

            return $"This animal is used by {string.Join(" and ", parts)} and cannot be deleted.";
        }

        private static string CountLabel(int count, string singular, string plural)
        {
            return $"{count} {(count == 1 ? singular : plural)}";
        }

        private async Task ValidateInsertAsync(AnimalInsertRequest request)
        {
            var result = await _insertValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task ValidateUpdateAsync(AnimalUpdateRequest request)
        {
            var result = await _updateValidator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ValidationException(result.Errors);
            }
        }

        private async Task EnsureCategoryExistsAsync(int categoryId)
        {
            if (!await _dbContext.AnimalCategories.AnyAsync(category => category.Id == categoryId))
            {
                ThrowNotFound("AnimalCategory", categoryId, "Category not found.");
            }
        }

        protected override IQueryable<Animal> ApplyFilters(IQueryable<Animal> query, AnimalSearchObject? search)
        {
            if (search != null)
            {
                if (search.CategoryId.HasValue)
                {
                    query = query.Where(a => a.CategoryId == search.CategoryId.Value);
                }

                if (search.Tag.HasValue)
                {
                    query = query.Where(a => a.Tag == search.Tag.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name.Trim();
                    query = query.Where(a => a.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(search.FTS))
                {
                    var term = search.FTS.Trim();
                    query = query.Where(a => a.Name.Contains(term) || a.Species.Contains(term));
                }
            }

            return query;
        }

        protected override IQueryable<Animal> ApplyDefaultOrder(IQueryable<Animal> query)
        {
            return query.OrderBy(a => a.Name).ThenBy(a => a.Id);
        }
    }
}
