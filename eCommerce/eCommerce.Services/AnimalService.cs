using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class AnimalService : BaseCRUDService<Animal, AnimalResponse, AnimalSearchObject, AnimalInsertRequest, AnimalUpdateRequest>, IAnimalService
    {
        public AnimalService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<AnimalInsertRequest> insertValidator, IValidator<AnimalUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

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
                throw new KeyNotFoundException($"Animal with id {id} not found.");
            }

            return _mapper.Map<AnimalResponse>(entity);
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
                throw new KeyNotFoundException($"AnimalCategory with id {categoryId} not found.");
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
