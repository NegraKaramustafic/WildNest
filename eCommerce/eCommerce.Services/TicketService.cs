using System;
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
        private const string AdminRole = "Admin";
        private const string CodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const int CodeLength = 6;
        private const int MaxCodeAttempts = 20;

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly INotificationEventService _notificationEvents;

        public TicketService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<TicketInsertRequest> insertValidator,
            IValidator<TicketUpdateRequest> updateValidator,
            IAuthenticatedUserAccessor userAccessor,
            INotificationEventService notificationEvents)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _userAccessor = userAccessor;
            _notificationEvents = notificationEvents;
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
            var created = await base.InsertAsync(request);
            var response = await GetByIdAsync(created.Id);
            await _notificationEvents.NotifyTicketPurchasedAsync(response);
            return response;
        }

        public override async Task<TicketResponse> UpdateAsync(int id, TicketUpdateRequest request)
        {
            var entity = await _dbContext.Tickets.FindAsync(id);
            if (entity == null || !CanAccessTicket(entity.UserId))
            {
                throw new KeyNotFoundException($"Ticket with id {id} not found.");
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
            entity.Status = TicketStatus.Active;
            entity.Code = GenerateUniqueCode();
            entity.QrCodeData = entity.Code;
            return entity;
        }

        protected override void MapUpdateRequestToEntity(TicketUpdateRequest request, Ticket entity)
        {
            var existingCode = entity.Code;
            var existingQr = entity.QrCodeData;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Code = existingCode;
            entity.QrCodeData = existingQr;
            entity.TotalPrice = request.PricePerPerson * request.VisitorCount;
        }

        protected override IEnumerable<Ticket> ApplyFilters(IEnumerable<Ticket> query, TicketSearchObject? search)
        {
            if (_userAccessor.IsInRole(AdminRole))
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

        public async Task<TicketResponse> VerifyAsync(TicketVerifyRequest request)
        {
            var code = request.Code?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ClinetException("Code is required.");
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
                throw new ClinetException("Ticket already used");
            }

            if (entity.Status == TicketStatus.Cancelled)
            {
                throw new ClinetException("Ticket is cancelled");
            }

            var verifierId = _userAccessor.GetUserId()
                ?? throw new InvalidOperationException("User id claim is missing.");

            entity.Status = TicketStatus.Used;
            entity.VerifiedAt = DateTime.UtcNow;
            entity.VerifiedByUserId = verifierId;
            entity.VerificationMethod = request.Method;
            await _dbContext.SaveChangesAsync();

            var response = await GetByIdAsync(entity.Id);
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (entity.VisitDate != today)
            {
                response.Warning = "Ticket is not valid for today";
            }

            await _notificationEvents.NotifyTicketVerifiedAsync(response);
            return response;
        }

        private bool CanAccessTicket(int ticketUserId)
        {
            if (_userAccessor.IsInRole(AdminRole))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == ticketUserId;
        }

        private string GenerateUniqueCode()
        {
            for (var attempt = 0; attempt < MaxCodeAttempts; attempt++)
            {
                var suffix = new char[CodeLength];
                for (var i = 0; i < CodeLength; i++)
                {
                    suffix[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
                }

                var code = $"WN-{new string(suffix)}";
                if (!_dbContext.Tickets.Any(t => t.Code == code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Unable to generate a unique ticket code.");
        }
    }
}
