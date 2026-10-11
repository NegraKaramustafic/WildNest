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
            var created = await base.InsertAsync(request);
            return await GetByIdAsync(created.Id);
        }

        public override async Task<AnimalResponse> UpdateAsync(int id, AnimalUpdateRequest request)
        {
            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        protected override Animal MapInsertRequestToEntity(AnimalInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.Category = FindCategory(entity.CategoryId)!;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(AnimalUpdateRequest request, Animal entity)
        {
            base.MapUpdateRequestToEntity(request, entity);
            entity.Category = FindCategory(entity.CategoryId)!;
        }

        private AnimalCategory? FindCategory(int categoryId)
        {
            if (categoryId <= 0)
            {
                return null;
            }

            return _dbContext.Set<AnimalCategory>().Find(categoryId);
        }

        protected override IEnumerable<Animal> ApplyFilters(IEnumerable<Animal> query, AnimalSearchObject? search)
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
                    query = query.Where(a =>
                        a.Name.Contains(search.FTS, StringComparison.OrdinalIgnoreCase) ||
                        a.Species.Contains(search.FTS, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }
    }
}
