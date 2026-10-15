using System;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class AnimalCategoryService : BaseCRUDService<AnimalCategory, AnimalCategoryResponse, AnimalCategorySearchObject, AnimalCategoryInsertRequest, AnimalCategoryUpdateRequest>, IAnimalCategoryService
    {
        public AnimalCategoryService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AnimalCategoryInsertRequest> insertValidator,
            IValidator<AnimalCategoryUpdateRequest> updateValidator,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
        }

        protected override string NotFoundMessage => "Category not found.";

        protected override IQueryable<AnimalCategory> ApplyFilters(IQueryable<AnimalCategory> query, AnimalCategorySearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    var name = search.Name.Trim();
                    query = query.Where(c => c.Name.Contains(name));
                }
            }

            return query;
        }

        protected override IQueryable<AnimalCategory> ApplyDefaultOrder(IQueryable<AnimalCategory> query)
        {
            return query.OrderBy(c => c.Name).ThenBy(c => c.Id);
        }
    }
}
