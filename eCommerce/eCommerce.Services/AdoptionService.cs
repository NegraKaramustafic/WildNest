using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class AdoptionService : BaseCRUDService<Adoption, AdoptionResponse, AdoptionSearchObject, AdoptionInsertRequest, AdoptionUpdateRequest>, IAdoptionService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IPaymentService _paymentService;

        public AdoptionService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AdoptionInsertRequest> insertValidator,
            IValidator<AdoptionUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            IPaymentService paymentService)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
            _paymentService = paymentService;
        }

        protected override async Task<IQueryable<Adoption>> IncludeRelatedEntitiesAsync(AdoptionSearchObject? search, IQueryable<Adoption> query = null!)
        {
            query = query
                .Include(a => a.User)
                .Include(a => a.Animal);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<AdoptionResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Adoption>()
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Animal)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            return _mapper.Map<AdoptionResponse>(entity);
        }

        public override async Task<AdoptionResponse> InsertAsync(AdoptionInsertRequest request)
        {
            await _insertValidator.ValidateAndThrowAsync(request);
            var animal = await _dbContext.Animals.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AnimalId);
            if (animal == null)
            {
                throw new KeyNotFoundException($"Animal with id {request.AnimalId} not found.");
            }

            if (!animal.AdoptionPrice.HasValue)
            {
                throw new ClientException("This animal is not available for adoption");
            }

            var entity = MapInsertRequestToEntity(request);
            entity.MonthlyAmount = animal.AdoptionPrice.Value;
            entity.StartDate = DateTime.UtcNow;
            _dbContext.Adoptions.Add(entity);
            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id);
        }

        public override async Task<AdoptionResponse> UpdateAsync(int id, AdoptionUpdateRequest request)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            await base.DeleteAsync(id);
        }

        protected override Adoption MapInsertRequestToEntity(AdoptionInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            entity.Status = AdoptionStatus.PendingPayment;
            entity.PaymentId = null;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(AdoptionUpdateRequest request, Adoption entity)
        {
            var existingStatus = entity.Status;
            var existingStartDate = entity.StartDate;
            var existingUserId = entity.UserId;
            var existingAnimalId = entity.AnimalId;
            var existingPaymentId = entity.PaymentId;
            var existingMonthlyAmount = entity.MonthlyAmount;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Status = existingStatus;
            entity.StartDate = existingStartDate;
            entity.UserId = existingUserId;
            entity.AnimalId = existingAnimalId;
            entity.PaymentId = existingPaymentId;
            entity.MonthlyAmount = existingMonthlyAmount;
        }

        protected override IQueryable<Adoption> ApplyFilters(IQueryable<Adoption> query, AdoptionSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(a => a.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(a => a.UserId == userId.Value)
                    : query.Where(a => false);
            }

            if (search != null)
            {
                if (search.AnimalId.HasValue)
                {
                    query = query.Where(a => a.AnimalId == search.AnimalId.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(a => a.Status == search.Status.Value);
                }
            }

            return query;
        }

        protected override IQueryable<Adoption> ApplyDefaultOrder(IQueryable<Adoption> query)
        {
            return query.OrderByDescending(a => a.StartDate).ThenByDescending(a => a.Id);
        }

        public async Task<AdoptionResponse> CancelAsync(int id)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                throw new KeyNotFoundException($"Adoption with id {id} not found.");
            }

            if (entity.Status == AdoptionStatus.Cancelled)
            {
                throw new ClientException("Adoption is already cancelled.");
            }

            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                entity.Status = AdoptionStatus.Cancelled;
                if (entity.PaymentId.HasValue)
                {
                    await _paymentService.HandleEntityCancellationAsync(entity.PaymentId.Value, refundIfCompleted: false);
                }

                await _dbContext.SaveChangesAsync();
            });

            return await GetByIdAsync(id);
        }

        private bool CanAccessAdoption(int adoptionUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == adoptionUserId;
        }
    }
}
