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
    public class BadgeService : BaseCRUDService<Badge, BadgeResponse, BadgeSearchObject, BadgeInsertRequest, BadgeUpdateRequest>, IBadgeService
    {
        public BadgeService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<BadgeInsertRequest> insertValidator,
            IValidator<BadgeUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override IEnumerable<Badge> ApplyFilters(IEnumerable<Badge> query, BadgeSearchObject? search)
        {
            if (search != null)
            {
                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(b => b.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (search.Criteria.HasValue)
                {
                    query = query.Where(b => b.Criteria == search.Criteria.Value);
                }
            }

            return query;
        }
    }
}
