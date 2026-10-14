using System;
using eCommerce.Model;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    public class TicketService : BaseCRUDService<Ticket, TicketResponse, TicketSearchObject, TicketInsertRequest, TicketUpdateRequest>, ITicketService
    {
        private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private const int MaxCodeAttempts = 20;

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly INotificationEventService _notificationEvents;
        private readonly IPaymentService _paymentService;
        private readonly ZooClock _zooClock;

        public TicketService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<TicketInsertRequest> insertValidator,
            IValidator<TicketUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            INotificationEventService notificationEvents,
            IPaymentService paymentService,
            ZooClock zooClock)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
            _notificationEvents = notificationEvents;
            _paymentService = paymentService;
            _zooClock = zooClock;
        }

        protected override async Task<IQueryable<Ticket>> IncludeRelatedEntitiesAsync(TicketSearchObject? search, IQueryable<Ticket> query = null!)
        {
            query = query
                .Include(t => t.User)
                .Include(t => t.VerifiedByUser);
            return await base.IncludeRelatedEntitiesAsync(search, query);
        }

        public override async Task<TicketResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Set<Ticket>()
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.VerifiedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null || !CanAccessTicket(entity.UserId))
            {
                throw new KeyNotFoundException($"Ticket with id {id} not found.");
            }

            return _mapper.Map<TicketResponse>(entity);
        }

        public override async Task<TicketResponse> InsertAsync(TicketInsertRequest request)
        {
            await _insertValidator.ValidateAndThrowAsync(request);
            var entity = MapInsertRequestToEntity(request);
            entity.Code = await GenerateUniqueCodeAsync();
            entity.QrCodeData = entity.Code;
            entity.CreatedAt = DateTime.UtcNow;
            _dbContext.Tickets.Add(entity);
            await _dbContext.SaveChangesAsync();
            return await GetByIdAsync(entity.Id);
        }

        public override async Task<TicketResponse> UpdateAsync(int id, TicketUpdateRequest request)
        {
            var entity = await _dbContext.Tickets.FindAsync(id);
            if (entity == null || !CanAccessTicket(entity.UserId))
            {
                throw new KeyNotFoundException($"Ticket with id {id} not found.");
            }

            if (entity.Status == TicketStatus.Used || entity.Status == TicketStatus.Cancelled)
            {
                throw new ClientException("A used or cancelled ticket cannot be edited.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            var entity = await _dbContext.Tickets.FindAsync(id);
            if (entity == null || !CanAccessTicket(entity.UserId))
            {
                throw new KeyNotFoundException($"Ticket with id {id} not found.");
            }

            await base.DeleteAsync(id);
        }

        protected override Ticket MapInsertRequestToEntity(TicketInsertRequest request)
        {
            var entity = base.MapInsertRequestToEntity(request);
            entity.UserId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");
            entity.PricePerPerson = ZooPricing.ZooEntryPricePerPerson;
            entity.TotalPrice = entity.PricePerPerson * request.VisitorCount;
            entity.Status = TicketStatus.PendingPayment;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(TicketUpdateRequest request, Ticket entity)
        {
            var existingCode = entity.Code;
            var existingQr = entity.QrCodeData;
            var existingStatus = entity.Status;
            var existingPricePerPerson = entity.PricePerPerson;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Code = existingCode;
            entity.QrCodeData = existingQr;
            entity.Status = existingStatus;
            entity.PricePerPerson = existingPricePerPerson;
            entity.TotalPrice = existingPricePerPerson * entity.VisitorCount;
        }

        protected override IQueryable<Ticket> ApplyFilters(IQueryable<Ticket> query, TicketSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(t => t.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(t => t.UserId == userId.Value)
                    : query.Where(t => false);
            }

            if (search != null)
            {
                if (search.Status.HasValue)
                {
                    query = query.Where(t => t.Status == search.Status.Value);
                }

                if (search.VisitDateFrom.HasValue)
                {
                    query = query.Where(t => t.VisitDate >= search.VisitDateFrom.Value);
                }

                if (search.VisitDateTo.HasValue)
                {
                    query = query.Where(t => t.VisitDate <= search.VisitDateTo.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.Code))
                {
                    var code = search.Code.Trim().ToUpperInvariant();
                    query = query.Where(t => t.Code == code);
                }
            }

            return query;
        }

        protected override IQueryable<Ticket> ApplyDefaultOrder(IQueryable<Ticket> query)
        {
            return query.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id);
        }

        public async Task<TicketResponse> VerifyAsync(TicketVerifyRequest request)
        {
            var code = request.Code?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ClientException("Code is required.");
            }

            var entity = await _dbContext.Tickets
                .Include(t => t.User)
                .Include(t => t.VerifiedByUser)
                .FirstOrDefaultAsync(t => t.Code == code);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Ticket with code {code} not found.");
            }

            if (entity.Status == TicketStatus.Used)
            {
                throw new ClientException("Ticket already used");
            }

            if (entity.Status == TicketStatus.Cancelled)
            {
                throw new ClientException("Ticket is cancelled");
            }

            if (entity.Status == TicketStatus.PendingPayment || !await HasCompletedPaymentAsync(entity.PaymentId))
            {
                throw new ClientException("Ticket payment is not completed.");
            }

            EnsureVisitIsToday(entity.VisitDate);

            var verifierId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            entity.Status = TicketStatus.Used;
            entity.VerifiedAt = DateTime.UtcNow;
            entity.VerifiedByUserId = verifierId;
            entity.VerificationMethod = request.Method;
            await _dbContext.SaveChangesAsync();

            var response = await GetByIdAsync(entity.Id);
            await _notificationEvents.NotifyTicketVerifiedAsync(response);
            return response;
        }

        private DateOnly Today() => _zooClock.Today;

        private void EnsureVisitIsToday(DateOnly visitDate)
        {
            var today = Today();
            if (visitDate < today)
            {
                throw new ClientException($"Ticket expired. It was valid for {visitDate:dd.MM.yyyy}.");
            }

            if (visitDate > today)
            {
                throw new ClientException($"Ticket is valid for {visitDate:dd.MM.yyyy}, not today.");
            }
        }

        private bool CanAccessTicket(int ticketUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == ticketUserId;
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
            {
                var suffix = new char[CodeLength];
                for (var i = 0; i < CodeLength; i++)
                {
                    suffix[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
                }

                var code = $"WN-{new string(suffix)}";
                if (!await _dbContext.Tickets.AnyAsync(t => t.Code == code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique ticket code.");
        }

        public async Task<TicketResponse> CancelAsync(int id)
        {
            var entity = await _dbContext.Tickets.FindAsync(id);
            if (entity == null || !CanAccessTicket(entity.UserId))
            {
                throw new KeyNotFoundException($"Ticket with id {id} not found.");
            }

            if (entity.Status == TicketStatus.Cancelled)
            {
                throw new ClientException("Ticket is already cancelled.");
            }

            if (entity.Status == TicketStatus.Used)
            {
                throw new ClientException("A used ticket cannot be cancelled.");
            }

            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                entity.Status = TicketStatus.Cancelled;
                if (entity.PaymentId.HasValue)
                {
                    await _paymentService.HandleEntityCancellationAsync(entity.PaymentId.Value);
                }

                await _dbContext.SaveChangesAsync();
            });

            return await GetByIdAsync(id);
        }

        private async Task<bool> HasCompletedPaymentAsync(int? paymentId)
        {
            if (!paymentId.HasValue)
            {
                return false;
            }

            return await _dbContext.Payments.AnyAsync(p => p.Id == paymentId.Value && p.Status == PaymentStatus.Completed);
        }
    }
}
