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
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class AdoptionService : BaseCRUDService<Adoption, AdoptionResponse, AdoptionSearchObject, AdoptionInsertRequest, AdoptionUpdateRequest>, IAdoptionService
    {

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly IPaymentService _paymentService;
        private readonly INotificationEventService _notificationEvents;
        private readonly IValidator<AdoptionRejectRequest> _rejectValidator;

        public AdoptionService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<AdoptionInsertRequest> insertValidator,
            IValidator<AdoptionUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            IPaymentService paymentService,
            INotificationEventService notificationEvents,
            IValidator<AdoptionRejectRequest> rejectValidator,
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _userAccessor = userAccessor;
            _paymentService = paymentService;
            _notificationEvents = notificationEvents;
            _rejectValidator = rejectValidator;
        }

        protected override string NotFoundMessage => "Adoption not found.";

        protected override async Task<IQueryable<Adoption>> IncludeRelatedEntitiesAsync(AdoptionSearchObject? search, IQueryable<Adoption> query = null!)
        {
            query = query
                .Include(a => a.User)
                .Include(a => a.Animal)
                .Include(a => a.Payment)
                .Include(a => a.RejectedByUser);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<AdoptionResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Adoption>()
                .AsNoTracking()
                .Include(a => a.User)
                .Include(a => a.Animal)
                .Include(a => a.Payment)
                .Include(a => a.RejectedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                ThrowNotFound("Adoption", id, "Adoption not found.");
            }

            var response = _mapper.Map<AdoptionResponse>(entity);
            response.AllowedActions = AllowedActions(entity.Status);
            return response;
        }

        public override async Task<PageResult<AdoptionResponse>> GetAllAsync(AdoptionSearchObject? search = null)
        {
            var page = await base.GetAllAsync(search);
            foreach (var item in page.Items)
            {
                item.AllowedActions = AllowedActions(item.Status);
            }

            return page;
        }

        public override async Task<AdoptionResponse> InsertAsync(AdoptionInsertRequest request)
        {
            await _insertValidator.ValidateAndThrowAsync(request);
            var animal = await _dbContext.Animals.AsNoTracking().FirstOrDefaultAsync(a => a.Id == request.AnimalId);
            if (animal == null)
            {
                ThrowNotFound("Animal", request.AnimalId, "Animal not found.");
            }

            if (!animal.AdoptionPrice.HasValue)
            {
                throw new ClientException("This animal is not available for adoption.");
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
                ThrowNotFound("Adoption", id, "Adoption not found.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Adoptions.FindAsync(id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                ThrowNotFound("Adoption", id, "Adoption not found.");
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

                EnsureDateRange(search.DateFrom, search.DateTo);

                if (search.DateFrom.HasValue)
                {
                    var from = search.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    query = query.Where(a => a.StartDate >= from);
                }

                if (search.DateTo.HasValue)
                {
                    var toExclusive = search.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                    query = query.Where(a => a.StartDate < toExclusive);
                }

                if (!string.IsNullOrWhiteSpace(search.FTS))
                {
                    var term = search.FTS.Trim();
                    query = query.Where(a =>
                        a.User.FirstName.Contains(term)
                        || a.User.LastName.Contains(term)
                        || (a.User.FirstName + " " + a.User.LastName).Contains(term)
                        || a.Animal.Name.Contains(term));
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
                ThrowNotFound("Adoption", id, "Adoption not found.");
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
                    // A completed adoption payment is a non-refundable contribution by design.
                    await _paymentService.HandleEntityCancellationAsync(entity.PaymentId.Value, refundIfCompleted: false);
                }

                await _dbContext.SaveChangesAsync();
            });

            return await GetByIdAsync(id);
        }

        public async Task<AdoptionResponse> ApproveAsync(int id)
        {
            throw await ApproveNotAllowedAsync(id);
        }

        public async Task<AdoptionResponse> RejectAsync(int id, AdoptionRejectRequest request)
        {
            EnsureAdmin(id);
            await ThrowFirstErrorAsync(_rejectValidator, request);

            var entity = await _dbContext.Adoptions
                .Include(a => a.User)
                .Include(a => a.Animal)
                .Include(a => a.Payment)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null || !CanAccessAdoption(entity.UserId))
            {
                ThrowNotFound("Adoption", id, "Adoption not found.");
            }

            if (entity.Status == AdoptionStatus.Cancelled)
            {
                throw new ClientException("This adoption is already cancelled.");
            }

            var ownsTransaction = _dbContext.Database.CurrentTransaction == null;
            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                entity.Status = AdoptionStatus.Cancelled;
                entity.RejectedByUserId = RequireAdminUserId();
                entity.RejectedAt = DateTime.UtcNow;
                entity.RejectionReason = request.Reason.Trim();
                if (entity.PaymentId.HasValue)
                {
                    // A completed adoption payment is a non-refundable contribution by design.
                    await _paymentService.HandleEntityCancellationAsync(entity.PaymentId.Value, refundIfCompleted: false);
                }

                await _dbContext.SaveChangesAsync();
            });

            var rejected = await GetByIdAsync(id);
            if (ownsTransaction)
            {
                await _notificationEvents.NotifyAdoptionRejectedAsync(rejected);
            }

            return rejected;
        }

        private async Task<Exception> ApproveNotAllowedAsync(int id)
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                _logger.LogWarning("Adoption {Id} is not accessible.", id);
                return new NotFoundException("Adoption not found.");
            }

            var status = await _dbContext.Adoptions.AsNoTracking()
                .Where(a => a.Id == id)
                .Select(a => (AdoptionStatus?)a.Status)
                .FirstOrDefaultAsync();
            if (status == null)
            {
                _logger.LogWarning("Adoption {Id} was not found.", id);
                return new NotFoundException("Adoption not found.");
            }

            return status switch
            {
                AdoptionStatus.PendingPayment => new ClientException("This adoption cannot be approved by an administrator. It becomes active when payment is completed."),
                AdoptionStatus.Active => new ClientException("This adoption is already active."),
                AdoptionStatus.Cancelled => new ClientException("A cancelled adoption cannot be approved."),
                _ => new ClientException("This adoption cannot be approved.")
            };
        }

        private static List<string> AllowedActions(AdoptionStatus status)
        {
            if (status == AdoptionStatus.Cancelled)
            {
                return new List<string>();
            }

            return new List<string> { nameof(RejectAsync) };
        }

        private void EnsureAdmin(int id)
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                _logger.LogWarning("Adoption {Id} is not accessible.", id);
                throw new NotFoundException("Adoption not found.");
            }
        }

        private int RequireAdminUserId()
        {
            return _userAccessor.GetUserId()
                ?? throw new ClientException("Administrator id is missing.");
        }

        private static void EnsureDateRange(DateOnly? from, DateOnly? to)
        {
            if (from.HasValue && to.HasValue && from.Value > to.Value)
            {
                throw new ClientException("From date cannot be after to date.");
            }
        }

        private static async Task ThrowFirstErrorAsync<T>(IValidator<T> validator, T request)
        {
            var result = await validator.ValidateAsync(request);
            if (!result.IsValid)
            {
                throw new ClientException(result.Errors[0].ErrorMessage);
            }
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
