using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services
{
    public class AnimalCategoryService : BaseCRUDService<AnimalCategory, AnimalCategoryResponse, AnimalCategorySearchObject, AnimalCategoryInsertRequest, AnimalCategoryUpdateRequest>, IAnimalCategoryService
    {
        public AnimalCategoryService(ECommerceDbContext dbContext, MapsterMapper.IMapper mapper, IValidator<AnimalCategoryInsertRequest> insertValidator, IValidator<AnimalCategoryUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<AnimalCategory> ApplyFilters(IEnumerable<AnimalCategory> query, AnimalCategorySearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(c => c.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }
    }
}
