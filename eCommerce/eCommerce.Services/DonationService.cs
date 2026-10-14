using System;
using eCommerce.Model;
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
    public class DonationService : BaseCRUDService<Donation, DonationResponse, DonationSearchObject, DonationInsertRequest, DonationUpdateRequest>, IDonationService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;

        public DonationService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<DonationInsertRequest> insertValidator,
            IValidator<DonationUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
        }

        protected override async Task<IQueryable<Donation>> IncludeRelatedEntitiesAsync(DonationSearchObject? search, IQueryable<Donation> query = null!)
        {
            query = query
                .Include(d => d.User)
                .Include(d => d.ZooPartner);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<DonationResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Donation>()
                .AsNoTracking()
                .Include(d => d.User)
                .Include(d => d.ZooPartner)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (entity == null || !CanAccessDonation(entity.UserId))
            {
                throw new KeyNotFoundException($"Donation with id {id} not found.");
            }

            return _mapper.Map<DonationResponse>(entity);
        }

        public override async Task<DonationResponse> InsertAsync(DonationInsertRequest request)
        {
            if (!await _dbContext.ZooPartners.AnyAsync(partner => partner.Id == request.ZooPartnerId))
            {
                throw new KeyNotFoundException($"ZooPartner with id {request.ZooPartnerId} not found.");
            }

            var created = await base.InsertAsync(request);
            return await GetByIdAsync(created.Id);
        }

        public override async Task<DonationResponse> UpdateAsync(int id, DonationUpdateRequest request)
        {
            var entity = await _dbContext.Donations.FindAsync(id);
            if (entity == null || !CanAccessDonation(entity.UserId))
            {
                throw new KeyNotFoundException($"Donation with id {id} not found.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Donations.FindAsync(id);
            if (entity == null || !CanAccessDonation(entity.UserId))
            {
                throw new KeyNotFoundException($"Donation with id {id} not found.");
            }

            await base.DeleteAsync(id);
        }

        protected override Donation MapInsertRequestToEntity(DonationInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            entity.PaymentId = null;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(DonationUpdateRequest request, Donation entity)
        {
            var existingUserId = entity.UserId;
            var existingZooPartnerId = entity.ZooPartnerId;
            var existingAmount = entity.Amount;
            var existingCreatedAt = entity.CreatedAt;
            var existingPaymentId = entity.PaymentId;
            base.MapUpdateRequestToEntity(request, entity);
            entity.UserId = existingUserId;
            entity.ZooPartnerId = existingZooPartnerId;
            entity.Amount = existingAmount;
            entity.CreatedAt = existingCreatedAt;
            entity.PaymentId = existingPaymentId;
        }

        protected override IQueryable<Donation> ApplyFilters(IQueryable<Donation> query, DonationSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(d => d.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(d => d.UserId == userId.Value)
                    : query.Where(d => false);
            }

            if (search?.ZooPartnerId.HasValue == true)
            {
                query = query.Where(d => d.ZooPartnerId == search.ZooPartnerId.Value);
            }

            return query;
        }

        protected override IQueryable<Donation> ApplyDefaultOrder(IQueryable<Donation> query)
        {
            return query.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id);
        }

        private bool CanAccessDonation(int donationUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == donationUserId;
        }
    }
}
