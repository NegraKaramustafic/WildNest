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
    public class ZooPartnerService : BaseCRUDService<ZooPartner, ZooPartnerResponse, ZooPartnerSearchObject, ZooPartnerInsertRequest, ZooPartnerUpdateRequest>, IZooPartnerService
    {
        public ZooPartnerService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<ZooPartnerInsertRequest> insertValidator,
            IValidator<ZooPartnerUpdateRequest> updateValidator)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
        }

        protected override ZooPartner MapInsertRequestToEntity(ZooPartnerInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.CurrentAmount = 0;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(ZooPartnerUpdateRequest request, ZooPartner entity)
        {
            var existingCurrentAmount = entity.CurrentAmount;
            base.MapUpdateRequestToEntity(request, entity);
            entity.CurrentAmount = existingCurrentAmount;
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
            }

            return query;
        }

        protected override IQueryable<ZooPartner> ApplyDefaultOrder(IQueryable<ZooPartner> query)
        {
            return query.OrderBy(p => p.Name).ThenBy(p => p.Id);
        }
    }
}
