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

        protected override IEnumerable<ZooPartner> ApplyFilters(IEnumerable<ZooPartner> query, ZooPartnerSearchObject? search)
        {
            if (search != null)
            {
                if (search.IsActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == search.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.Name))
                {
                    query = query.Where(p => p.Name.Contains(search.Name, StringComparison.OrdinalIgnoreCase));
                }
            }

            return query;
        }
    }
}
