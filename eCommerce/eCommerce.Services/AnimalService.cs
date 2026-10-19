using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class AnimalService : BaseCRUDService<Animal, AnimalResponse, AnimalSearchObject, AnimalInsertRequest, AnimalUpdateRequest>, IAnimalService
    {
        public AnimalService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AnimalInsertRequest> insertValidator,
            IValidator<AnimalUpdateRequest> updateValidator,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
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
            var counts = await _dbContext.Adoptions
                .AsNoTracking()
                .Where(adoption => ids.Contains(adoption.AnimalId) && adoption.Status == AdoptionStatus.Active)
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
            var created = await base.InsertAsync(request);
            return await GetByIdAsync(created.Id);
        }

        public override async Task<AnimalResponse> UpdateAsync(int id, AnimalUpdateRequest request)
        {
            await EnsureCategoryExistsAsync(request.CategoryId);
            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
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
