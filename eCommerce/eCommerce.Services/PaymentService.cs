using System.Collections.Generic;
using eCommerce.Model;
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
using Stripe;
using Stripe.Checkout;

namespace eCommerce.Services
{
    public class PaymentService : BaseCRUDService<Database.Payment, PaymentResponse, PaymentSearchObject, PaymentInsertRequest, PaymentUpdateRequest>, IPaymentService
    {
        private const string MetadataPaymentId = "paymentId";
        private const string MetadataEntityType = "relatedEntityType";
        private const string MetadataEntityId = "relatedEntityId";

        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly INotificationEventService _notificationEvents;
        private readonly IBadgeAwardService _badgeAwardService;
        private readonly StripeSettings _stripeSettings;
        private readonly IValidator<PaymentCheckoutRequest> _checkoutValidator;
        private readonly ILogger<PaymentService> _logger;
        private readonly StripeClient _stripeClient;

        public PaymentService(
            ECommerceDbContext dbContext,
            MapsterMapper.IMapper mapper,
            IValidator<PaymentInsertRequest> insertValidator,
            IValidator<PaymentUpdateRequest> updateValidator,
            IValidator<PaymentCheckoutRequest> checkoutValidator,
            IAuthenticatedUserAccessor userAccessor,
            INotificationEventService notificationEvents,
            IBadgeAwardService badgeAwardService,
            StripeSettings stripeSettings,
            ILogger<PaymentService> logger)
            : base(dbContext, mapper, insertValidator, updateValidator)
        {
            _checkoutValidator = checkoutValidator;
            _userAccessor = userAccessor;
            _notificationEvents = notificationEvents;
            _badgeAwardService = badgeAwardService;
            _stripeSettings = stripeSettings;
            _logger = logger;
            _stripeClient = new StripeClient(_stripeSettings.SecretKey);
        }

        public override async Task<PaymentResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null || !CanAccessPayment(entity.UserId))
            {
                throw new KeyNotFoundException($"Payment with id {id} not found.");
            }

            return _mapper.Map<PaymentResponse>(entity);
        }

        public override Task<PaymentResponse> InsertAsync(PaymentInsertRequest request)
        {
            throw new ClinetException("Payments are created through POST /api/Payment/checkout. The amount is taken from the related record.");
        }

        public override async Task<PaymentResponse> UpdateAsync(int id, PaymentUpdateRequest request)
        {
            EnsureAdmin();
            var entity = await GetEntityOrThrowAsync(id);
            if (!CanAccessPayment(entity.UserId))
            {
                throw new KeyNotFoundException($"Payment with id {id} not found.");
            }

            await base.UpdateAsync(id, request);
            return await GetByIdAsync(id);
        }

        public override async Task DeleteAsync(int id)
        {
            EnsureAdmin();
            var entity = await GetEntityOrThrowAsync(id);
            if (!CanAccessPayment(entity.UserId))
            {
                throw new KeyNotFoundException($"Payment with id {id} not found.");
            }

            await base.DeleteAsync(id);
        }

        protected override void MapUpdateRequestToEntity(PaymentUpdateRequest request, Database.Payment entity)
        {
            var existingStatus = entity.Status;
            var existingAmount = entity.Amount;
            var existingUserId = entity.UserId;
            var existingRelatedType = entity.RelatedEntityType;
            var existingRelatedId = entity.RelatedEntityId;
            var existingIntent = entity.StripePaymentIntentId;
            var existingSession = entity.StripeCheckoutSessionId;
            var existingRefund = entity.StripeRefundId;
            base.MapUpdateRequestToEntity(request, entity);
            entity.Status = existingStatus;
            entity.Amount = existingAmount;
            entity.UserId = existingUserId;
            entity.RelatedEntityType = existingRelatedType;
            entity.RelatedEntityId = existingRelatedId;
            entity.StripePaymentIntentId = existingIntent;
            entity.StripeCheckoutSessionId = existingSession;
            entity.StripeRefundId = existingRefund;
        }

        protected override IEnumerable<Database.Payment> ApplyFilters(IEnumerable<Database.Payment> query, PaymentSearchObject? search)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                if (search?.UserId.HasValue == true)
                {
                    query = query.Where(p => p.UserId == search.UserId.Value);
                }
            }
            else
            {
                var userId = _userAccessor.GetUserId();
                query = userId.HasValue
                    ? query.Where(p => p.UserId == userId.Value)
                    : query.Where(p => false);
            }

            if (search != null)
            {
                if (search.Method.HasValue)
                {
                    query = query.Where(p => p.Method == search.Method.Value);
                }

                if (search.Status.HasValue)
                {
                    query = query.Where(p => p.Status == search.Status.Value);
                }
            }

            return query;
        }

        public async Task<PaymentCheckoutResponse> CreateCheckoutAsync(PaymentCheckoutRequest request)
        {
            var validationResult = await _checkoutValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var payable = await ResolvePayableAsync(request.RelatedEntityType, request.RelatedEntityId);
            if (payable.ExistingPaymentId.HasValue)
            {
                var existing = await _dbContext.Payments.FindAsync(payable.ExistingPaymentId.Value);
                if (existing != null)
                {
                    if (existing.Status == PaymentStatus.Completed)
                    {
                        throw new ClinetException("This record already has a completed payment.");
                    }

                    if (existing.Status == PaymentStatus.Pending)
                    {
                        var reused = await TryReuseOpenSessionAsync(existing, request.RelatedEntityType, request.RelatedEntityId);
                        if (reused != null)
                        {
                            return reused;
                        }

                        existing.Status = PaymentStatus.Failed;
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }

            var payment = new Database.Payment
            {
                Method = Model.Enums.PaymentMethod.Card,
                Amount = payable.Amount,
                Status = PaymentStatus.Pending,
                UserId = payable.UserId,
                RelatedEntityType = request.RelatedEntityType.ToString(),
                RelatedEntityId = request.RelatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Payments.Add(payment);
            await _dbContext.SaveChangesAsync();
            payable.AssignPaymentId(payment);
            await _dbContext.SaveChangesAsync();

            try
            {
                var session = await CreateStripeSessionAsync(payment, payable.Description);
                payment.StripeCheckoutSessionId = session.Id;
                payment.StripePaymentIntentId = session.PaymentIntentId;
                await _dbContext.SaveChangesAsync();

                return MapCheckoutResponse(payment, request.RelatedEntityType, request.RelatedEntityId, session);
            }
            catch (StripeException ex)
            {
                payment.Status = PaymentStatus.Failed;
                await _dbContext.SaveChangesAsync();
                _logger.LogError(ex, "Stripe checkout session creation failed for payment {PaymentId}.", payment.Id);
                throw new ClinetException("Unable to start Stripe checkout. Please try again.");
            }
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ClinetException("Webhook payload is empty.");
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                throw new ClinetException("Invalid Stripe webhook signature.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            if (await _dbContext.StripeProcessedEvents.AnyAsync(e => e.EventId == stripeEvent.Id))
            {
                await transaction.CommitAsync();
                return;
            }

            _dbContext.StripeProcessedEvents.Add(new StripeProcessedEvent
            {
                EventId = stripeEvent.Id,
                EventType = stripeEvent.Type,
                ProcessedAt = DateTime.UtcNow
            });

            try
            {
                await ApplyWebhookEventAsync(stripeEvent);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                if (!await EventAlreadyStoredAsync(stripeEvent.Id))
                {
                    throw;
                }
            }
        }

        public async Task<PaymentResponse> RefundAsync(int id)
        {
            EnsureAdmin();
            var entity = await GetEntityOrThrowAsync(id);
            await RefundCompletedPaymentAsync(entity);
            return _mapper.Map<PaymentResponse>(entity);
        }

        public async Task HandleEntityCancellationAsync(int paymentId, bool refundIfCompleted = true)
        {
            var payment = await _dbContext.Payments.FindAsync(paymentId);
            if (payment == null || payment.Status == PaymentStatus.Refunded || payment.Status == PaymentStatus.Failed)
            {
                return;
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                // Adoption passes false: completed adoption payments are non-refundable by design.
                if (refundIfCompleted)
                {
                    await RefundCompletedPaymentAsync(payment);
                }

                return;
            }

            if (payment.Status == PaymentStatus.Pending)
            {
                await CancelPendingStripeSessionAsync(payment);
                payment.Status = PaymentStatus.Failed;
                await _dbContext.SaveChangesAsync();
            }
        }

        private async Task ApplyWebhookEventAsync(Event stripeEvent)
        {
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                var payment = await FindPaymentFromSessionAsync(session);
                if (payment != null)
                {
                    if (session != null && string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
                    {
                        payment.StripePaymentIntentId = session.PaymentIntentId;
                    }

                    await CompletePaymentAsync(payment);
                }

                return;
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                var payment = await FindPaymentFromIntentAsync(intent);
                if (payment != null)
                {
                    await CompletePaymentAsync(payment);
                }

                return;
            }

            if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
            {
                var intent = stripeEvent.Data.Object as PaymentIntent;
                var payment = await FindPaymentFromIntentAsync(intent);
                if (payment != null && payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                }
            }
        }

        private async Task CompletePaymentAsync(Database.Payment payment)
        {
            if (payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Refunded)
            {
                return;
            }

            if (payment.Status == PaymentStatus.Failed)
            {
                _logger.LogInformation(
                    "Payment {PaymentId} succeeded after it was marked failed; issuing a refund instead of applying effects.",
                    payment.Id);
                await RefundCompletedPaymentAsync(payment);
                return;
            }

            if (await RelatedEntityIsCancelledAsync(payment))
            {
                payment.Status = PaymentStatus.Completed;
                await _dbContext.SaveChangesAsync();
                await RefundCompletedPaymentAsync(payment);
                return;
            }

            payment.Status = PaymentStatus.Completed;
            var userId = payment.UserId ?? await FindPaymentUserIdAsync(payment.Id);
            await ApplyPaidSideEffectsAsync(payment);
            await _dbContext.SaveChangesAsync();

            if (userId.HasValue)
            {
                await _notificationEvents.NotifyPaymentCompletedAsync(userId.Value, payment.Id, payment.Amount);
                if (payment.RelatedEntityType is "Reservation" or "Adoption" or "Donation")
                {
                    await _badgeAwardService.CheckAndAwardBadgesAsync(userId.Value);
                }
            }
        }

        private async Task ApplyPaidSideEffectsAsync(Database.Payment payment)
        {
            PaymentRelatedEntityType? relatedType = TryParseRelatedType(payment.RelatedEntityType, out var parsedType)
                ? parsedType
                : null;
            if (relatedType == null || !payment.RelatedEntityId.HasValue)
            {
                relatedType = await InferRelatedTypeAsync(payment.Id);
            }

            switch (relatedType)
            {
                case PaymentRelatedEntityType.Ticket:
                    var ticket = await _dbContext.Tickets
                        .Include(t => t.User)
                        .FirstOrDefaultAsync(t => t.PaymentId == payment.Id || t.Id == payment.RelatedEntityId);
                    if (ticket != null)
                    {
                        if (ticket.Status == TicketStatus.PendingPayment)
                        {
                            ticket.Status = TicketStatus.Active;
                        }

                        await _dbContext.SaveChangesAsync();
                        var ticketResponse = _mapper.Map<TicketResponse>(ticket);
                        await _notificationEvents.NotifyTicketPurchasedAsync(ticketResponse);
                    }

                    break;

                case PaymentRelatedEntityType.Adoption:
                    var adoption = await _dbContext.Adoptions
                        .Include(a => a.User)
                        .Include(a => a.Animal)
                        .FirstOrDefaultAsync(a => a.PaymentId == payment.Id || a.Id == payment.RelatedEntityId);
                    if (adoption != null)
                    {
                        if (adoption.Status == AdoptionStatus.PendingPayment)
                        {
                            adoption.Status = AdoptionStatus.Active;
                        }

                        await _dbContext.SaveChangesAsync();
                        var adoptionResponse = _mapper.Map<AdoptionResponse>(adoption);
                        adoptionResponse.UserFullName = adoption.User != null
                            ? $"{adoption.User.FirstName} {adoption.User.LastName}".Trim()
                            : string.Empty;
                        adoptionResponse.AnimalName = adoption.Animal?.Name ?? string.Empty;
                        await _notificationEvents.NotifyAdoptionCreatedAsync(adoptionResponse);
                    }

                    break;

                case PaymentRelatedEntityType.Donation:
                    var donation = await _dbContext.Donations
                        .Include(d => d.User)
                        .Include(d => d.ZooPartner)
                        .FirstOrDefaultAsync(d => d.PaymentId == payment.Id || d.Id == payment.RelatedEntityId);
                    if (donation?.ZooPartner != null)
                    {
                        donation.ZooPartner.CurrentAmount += donation.Amount;
                        await _dbContext.SaveChangesAsync();
                        var donationResponse = _mapper.Map<DonationResponse>(donation);
                        donationResponse.UserFullName = donation.User != null
                            ? $"{donation.User.FirstName} {donation.User.LastName}".Trim()
                            : string.Empty;
                        donationResponse.ZooPartnerName = donation.ZooPartner.Name;
                        await _notificationEvents.NotifyDonationReceivedAsync(donationResponse);
                    }

                    break;
            }
        }

        private async Task<bool> RelatedEntityIsCancelledAsync(Database.Payment payment)
        {
            if (!TryParseRelatedType(payment.RelatedEntityType, out var relatedType) || !payment.RelatedEntityId.HasValue)
            {
                return false;
            }

            return relatedType switch
            {
                PaymentRelatedEntityType.Ticket =>
                    await _dbContext.Tickets.AnyAsync(t => t.Id == payment.RelatedEntityId && t.Status == TicketStatus.Cancelled),
                PaymentRelatedEntityType.Reservation =>
                    await _dbContext.Reservations.AnyAsync(r => r.Id == payment.RelatedEntityId && r.Status == ReservationStatus.Cancelled),
                PaymentRelatedEntityType.Adoption =>
                    await _dbContext.Adoptions.AnyAsync(a => a.Id == payment.RelatedEntityId && a.Status == AdoptionStatus.Cancelled),
                _ => false
            };
        }

        private async Task RefundCompletedPaymentAsync(Database.Payment payment)
        {
            if (payment.Status == PaymentStatus.Refunded)
            {
                return;
            }

            if (payment.Status != PaymentStatus.Completed && payment.Status != PaymentStatus.Failed)
            {
                throw new ClinetException("Only a completed payment can be refunded.");
            }

            if (!string.IsNullOrWhiteSpace(payment.StripePaymentIntentId) && string.IsNullOrWhiteSpace(payment.StripeRefundId))
            {
                try
                {
                    var refundService = new RefundService(_stripeClient);
                    var refund = await refundService.CreateAsync(new RefundCreateOptions
                    {
                        PaymentIntent = payment.StripePaymentIntentId
                    });
                    payment.StripeRefundId = refund.Id;
                }
                catch (StripeException ex) when (IsAlreadyRefunded(ex))
                {
                    _logger.LogInformation(ex, "Stripe payment {PaymentIntentId} was already refunded.", payment.StripePaymentIntentId);
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Stripe refund failed for payment {PaymentId}.", payment.Id);
                    throw new ClinetException("Stripe refund failed. The payment was not marked as refunded.");
                }
            }

            payment.Status = PaymentStatus.Refunded;
            await _dbContext.SaveChangesAsync();
        }

        private async Task CancelPendingStripeSessionAsync(Database.Payment payment)
        {
            if (!string.IsNullOrWhiteSpace(payment.StripeCheckoutSessionId))
            {
                try
                {
                    var sessionService = new SessionService(_stripeClient);
                    var session = await sessionService.GetAsync(payment.StripeCheckoutSessionId);
                    if (session.Status == "complete" || session.PaymentStatus == "paid")
                    {
                        payment.Status = PaymentStatus.Completed;
                        await _dbContext.SaveChangesAsync();
                        await RefundCompletedPaymentAsync(payment);
                        return;
                    }

                    if (session.Status == "open")
                    {
                        await sessionService.ExpireAsync(payment.StripeCheckoutSessionId);
                    }
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Unable to expire Stripe session {SessionId} for payment {PaymentId}.", payment.StripeCheckoutSessionId, payment.Id);
                }
            }
        }

        private async Task<PaymentCheckoutResponse?> TryReuseOpenSessionAsync(
            Database.Payment existing,
            PaymentRelatedEntityType relatedType,
            int relatedEntityId)
        {
            if (string.IsNullOrWhiteSpace(existing.StripeCheckoutSessionId))
            {
                return null;
            }

            try
            {
                var sessionService = new SessionService(_stripeClient);
                var session = await sessionService.GetAsync(
                    existing.StripeCheckoutSessionId,
                    new SessionGetOptions { Expand = new List<string> { "payment_intent" } });

                if (session.Status == "open")
                {
                    if (string.IsNullOrWhiteSpace(existing.StripePaymentIntentId))
                    {
                        existing.StripePaymentIntentId = session.PaymentIntentId;
                        await _dbContext.SaveChangesAsync();
                    }

                    return MapCheckoutResponse(existing, relatedType, relatedEntityId, session);
                }
            }
            catch (StripeException ex)
            {
                _logger.LogInformation(ex, "Existing Stripe session {SessionId} cannot be reused.", existing.StripeCheckoutSessionId);
            }

            return null;
        }

        private async Task<Session> CreateStripeSessionAsync(Database.Payment payment, string description)
        {
            var metadata = new Dictionary<string, string>
            {
                [MetadataPaymentId] = payment.Id.ToString(),
                [MetadataEntityType] = payment.RelatedEntityType ?? string.Empty,
                [MetadataEntityId] = payment.RelatedEntityId?.ToString() ?? string.Empty
            };

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = _stripeSettings.SuccessUrl,
                CancelUrl = _stripeSettings.CancelUrl,
                ClientReferenceId = payment.Id.ToString(),
                Metadata = metadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = metadata,
                    Description = description
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = _stripeSettings.Currency,
                            UnitAmount = ToCents(payment.Amount),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = description
                            }
                        }
                    }
                },
                Expand = new List<string> { "payment_intent" }
            };

            var sessionService = new SessionService(_stripeClient);
            return await sessionService.CreateAsync(options);
        }

        private PaymentCheckoutResponse MapCheckoutResponse(
            Database.Payment payment,
            PaymentRelatedEntityType relatedType,
            int relatedEntityId,
            Session session)
        {
            return new PaymentCheckoutResponse
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Currency = _stripeSettings.Currency,
                Status = payment.Status,
                ClientSecret = session.PaymentIntent?.ClientSecret,
                SessionUrl = session.Url,
                StripePaymentIntentId = session.PaymentIntentId ?? payment.StripePaymentIntentId,
                StripeCheckoutSessionId = session.Id,
                RelatedEntityType = relatedType,
                RelatedEntityId = relatedEntityId
            };
        }

        private async Task<PayableRecord> ResolvePayableAsync(PaymentRelatedEntityType type, int id)
        {
            switch (type)
            {
                case PaymentRelatedEntityType.Ticket:
                    {
                        var ticket = await _dbContext.Tickets.FindAsync(id);
                        if (ticket == null || !CanAccessOwnedRecord(ticket.UserId))
                        {
                            throw new KeyNotFoundException($"Ticket with id {id} not found.");
                        }

                        if (ticket.Status is TicketStatus.Cancelled or TicketStatus.Used)
                        {
                            throw new ClinetException("This ticket cannot be paid.");
                        }

                        return new PayableRecord(
                            ticket.UserId,
                            ticket.TotalPrice,
                            $"Zoo entry ticket {ticket.Code}",
                            ticket.PaymentId,
                            payment => ticket.PaymentId = payment.Id);
                    }

                case PaymentRelatedEntityType.Reservation:
                    {
                        var reservation = await _dbContext.Reservations.FindAsync(id);
                        if (reservation == null || !CanAccessOwnedRecord(reservation.UserId))
                        {
                            throw new KeyNotFoundException($"Reservation with id {id} not found.");
                        }

                        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed)
                        {
                            throw new ClinetException("This reservation cannot be paid.");
                        }

                        return new PayableRecord(
                            reservation.UserId,
                            reservation.TotalPrice,
                            $"Reservation {reservation.Code}",
                            reservation.PaymentId,
                            payment => reservation.PaymentId = payment.Id);
                    }

                case PaymentRelatedEntityType.Adoption:
                    {
                        var adoption = await _dbContext.Adoptions.FindAsync(id);
                        if (adoption == null || !CanAccessOwnedRecord(adoption.UserId))
                        {
                            throw new KeyNotFoundException($"Adoption with id {id} not found.");
                        }

                        if (adoption.Status == AdoptionStatus.Cancelled)
                        {
                            throw new ClinetException("This adoption cannot be paid.");
                        }

                        return new PayableRecord(
                            adoption.UserId,
                            adoption.MonthlyAmount,
                            $"Animal adoption #{adoption.Id}",
                            adoption.PaymentId,
                            payment => adoption.PaymentId = payment.Id);
                    }

                case PaymentRelatedEntityType.Donation:
                    {
                        var donation = await _dbContext.Donations.FindAsync(id);
                        if (donation == null || !CanAccessOwnedRecord(donation.UserId))
                        {
                            throw new KeyNotFoundException($"Donation with id {id} not found.");
                        }

                        return new PayableRecord(
                            donation.UserId,
                            donation.Amount,
                            $"Donation #{donation.Id}",
                            donation.PaymentId,
                            payment => donation.PaymentId = payment.Id);
                    }

                default:
                    throw new ClinetException("Unsupported related entity type.");
            }
        }

        private async Task<Database.Payment?> FindPaymentFromSessionAsync(Session? session)
        {
            if (session == null)
            {
                return null;
            }

            Database.Payment? payment = null;
            if (!string.IsNullOrWhiteSpace(session.Id))
            {
                payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.StripeCheckoutSessionId == session.Id);
            }

            if (payment == null && TryReadPaymentId(session.Metadata, session.ClientReferenceId, out var paymentId))
            {
                payment = await _dbContext.Payments.FindAsync(paymentId);
            }

            if (payment == null && !string.IsNullOrWhiteSpace(session.PaymentIntentId))
            {
                payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.StripePaymentIntentId == session.PaymentIntentId);
            }

            return payment;
        }

        private async Task<Database.Payment?> FindPaymentFromIntentAsync(PaymentIntent? intent)
        {
            if (intent == null)
            {
                return null;
            }

            var payment = await _dbContext.Payments.FirstOrDefaultAsync(p => p.StripePaymentIntentId == intent.Id);
            if (payment == null && TryReadPaymentId(intent.Metadata, null, out var paymentId))
            {
                payment = await _dbContext.Payments.FindAsync(paymentId);
            }

            return payment;
        }

        private static bool TryReadPaymentId(Dictionary<string, string>? metadata, string? clientReferenceId, out int paymentId)
        {
            if (metadata != null && metadata.TryGetValue(MetadataPaymentId, out var raw) && int.TryParse(raw, out paymentId))
            {
                return true;
            }

            return int.TryParse(clientReferenceId, out paymentId);
        }

        private async Task<int?> FindPaymentUserIdAsync(int paymentId)
        {
            return await _dbContext.Tickets.Where(t => t.PaymentId == paymentId).Select(t => (int?)t.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Reservations.Where(r => r.PaymentId == paymentId).Select(r => (int?)r.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Adoptions.Where(a => a.PaymentId == paymentId).Select(a => (int?)a.UserId).FirstOrDefaultAsync()
                ?? await _dbContext.Donations.Where(d => d.PaymentId == paymentId).Select(d => (int?)d.UserId).FirstOrDefaultAsync();
        }

        private async Task<PaymentRelatedEntityType?> InferRelatedTypeAsync(int paymentId)
        {
            if (await _dbContext.Tickets.AnyAsync(t => t.PaymentId == paymentId))
            {
                return PaymentRelatedEntityType.Ticket;
            }

            if (await _dbContext.Reservations.AnyAsync(r => r.PaymentId == paymentId))
            {
                return PaymentRelatedEntityType.Reservation;
            }

            if (await _dbContext.Adoptions.AnyAsync(a => a.PaymentId == paymentId))
            {
                return PaymentRelatedEntityType.Adoption;
            }

            if (await _dbContext.Donations.AnyAsync(d => d.PaymentId == paymentId))
            {
                return PaymentRelatedEntityType.Donation;
            }

            return null;
        }

        private async Task<Database.Payment> GetEntityOrThrowAsync(int id)
        {
            var entity = await _dbContext.Payments.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Payment with id {id} not found.");
            }

            return entity;
        }

        private async Task<bool> EventAlreadyStoredAsync(string eventId)
        {
            return await _dbContext.StripeProcessedEvents.AsNoTracking().AnyAsync(e => e.EventId == eventId);
        }

        private bool CanAccessPayment(int? paymentUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && paymentUserId.HasValue && userId.Value == paymentUserId.Value;
        }

        private bool CanAccessOwnedRecord(int ownerUserId)
        {
            if (_userAccessor.IsInRole(Roles.Admin))
            {
                return true;
            }

            var userId = _userAccessor.GetUserId();
            return userId.HasValue && userId.Value == ownerUserId;
        }

        private void EnsureAdmin()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                throw new UnauthorizedAccessException("Only an administrator can perform this payment action.");
            }
        }

        private static bool TryParseRelatedType(string? value, out PaymentRelatedEntityType relatedType)
        {
            return Enum.TryParse(value, ignoreCase: true, out relatedType);
        }

        private static long ToCents(decimal amount)
        {
            return (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
        }

        private static bool IsAlreadyRefunded(StripeException ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.Contains("already been refunded", StringComparison.OrdinalIgnoreCase)
                || message.Contains("charge_already_refunded", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class PayableRecord
        {
            public PayableRecord(
                int userId,
                decimal amount,
                string description,
                int? existingPaymentId,
                Action<Database.Payment> assignPaymentId)
            {
                UserId = userId;
                Amount = amount;
                Description = description;
                ExistingPaymentId = existingPaymentId;
                AssignPaymentId = assignPaymentId;
            }

            public int UserId { get; }
            public decimal Amount { get; }
            public string Description { get; }
            public int? ExistingPaymentId { get; }
            public Action<Database.Payment> AssignPaymentId { get; }
        }
    }
}
