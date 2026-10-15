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
            ILoggerFactory loggerFactory)
            : base(dbContext, mapper, insertValidator, updateValidator, loggerFactory)
        {
            _checkoutValidator = checkoutValidator;
            _userAccessor = userAccessor;
            _notificationEvents = notificationEvents;
            _badgeAwardService = badgeAwardService;
            _stripeSettings = stripeSettings;
            _stripeClient = new StripeClient(_stripeSettings.SecretKey);
        }

        protected override string NotFoundMessage => "Payment not found.";

        public override async Task<PaymentResponse> GetByIdAsync(int id)
        {
            var entity = await _dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null || !CanAccessPayment(entity.UserId))
            {
                ThrowNotFound("Payment", id, "Payment not found.");
            }

            return _mapper.Map<PaymentResponse>(entity);
        }

        public override Task<PaymentResponse> InsertAsync(PaymentInsertRequest request)
        {
            throw new ClientException("Payments are created through POST /api/Payment/checkout. The amount is taken from the related record.");
        }

        public override async Task<PaymentResponse> UpdateAsync(int id, PaymentUpdateRequest request)
        {
            EnsureAdmin();
            var entity = await GetEntityOrThrowAsync(id);
            if (!CanAccessPayment(entity.UserId))
            {
                ThrowNotFound("Payment", id, "Payment not found.");
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
                ThrowNotFound("Payment", id, "Payment not found.");
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

        protected override IQueryable<Database.Payment> ApplyFilters(IQueryable<Database.Payment> query, PaymentSearchObject? search)
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

        protected override IQueryable<Database.Payment> ApplyDefaultOrder(IQueryable<Database.Payment> query)
        {
            return query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id);
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
                        throw new ClientException("This record already has a completed payment.");
                    }

                    if (existing.Status == PaymentStatus.Pending)
                    {
                        var reused = await TryReuseOpenSessionAsync(existing, request.RelatedEntityType, request.RelatedEntityId);
                        if (reused != null)
                        {
                            return reused;
                        }

                        existing.Status = PaymentStatus.Failed;
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

            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync();
                payable.AssignPaymentId(payment);
                await _dbContext.SaveChangesAsync();
            });

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
                throw new ClientException("Unable to start Stripe checkout. Please try again.");
            }
        }

        public async Task HandleWebhookAsync(string json, string? stripeSignature)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ClientException("Webhook payload is empty.");
            }

            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
                throw new ClientException("Invalid Stripe webhook signature.");
            }

            if (await EventAlreadyStoredAsync(stripeEvent.Id))
            {
                return;
            }

            await ApplyWebhookEventAsync(stripeEvent);
            await RecordProcessedEventAsync(stripeEvent);
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
                if (refundIfCompleted)
                {
                    await RefundCompletedPaymentAsync(payment, persist: false);
                }

                return;
            }

            if (payment.Status == PaymentStatus.Pending)
            {
                await CancelPendingStripeSessionAsync(payment);
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Failed;
                }
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
                await DbTransactions.ExecuteAsync(_dbContext, async () =>
                {
                    payment.Status = PaymentStatus.Completed;
                    await RefundCompletedPaymentAsync(payment, persist: false);
                    await _dbContext.SaveChangesAsync();
                });
                return;
            }

            PaidCompletion completion;
            await using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                var claimed = await _dbContext.Payments
                    .Where(p => p.Id == payment.Id && p.Status == PaymentStatus.Pending)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.Status, PaymentStatus.Completed));

                if (claimed != 1)
                {
                    await transaction.RollbackAsync();
                    _logger.LogInformation(
                        "Payment {PaymentId} was not pending; another request already completed it. Skipping side effects.",
                        payment.Id);
                    return;
                }

                var statusEntry = _dbContext.Entry(payment).Property(p => p.Status);
                payment.Status = PaymentStatus.Completed;
                statusEntry.OriginalValue = PaymentStatus.Completed;
                statusEntry.IsModified = false;

                completion = await ApplyPaidSideEffectsAsync(payment);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            await PublishPaidCompletionAsync(payment, completion);
        }

        private async Task<PaidCompletion> ApplyPaidSideEffectsAsync(Database.Payment payment)
        {
            PaymentRelatedEntityType? relatedType = TryParseRelatedType(payment.RelatedEntityType, out var parsedType)
                ? parsedType
                : null;
            if (relatedType == null || !payment.RelatedEntityId.HasValue)
            {
                relatedType = await InferRelatedTypeAsync(payment.Id);
            }

            var completion = new PaidCompletion { RelatedType = relatedType };

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

                        completion.Ticket = _mapper.Map<TicketResponse>(ticket);
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

                        var adoptionResponse = _mapper.Map<AdoptionResponse>(adoption);
                        adoptionResponse.UserFullName = adoption.User != null
                            ? $"{adoption.User.FirstName} {adoption.User.LastName}".Trim()
                            : string.Empty;
                        adoptionResponse.AnimalName = adoption.Animal?.Name ?? string.Empty;
                        completion.Adoption = adoptionResponse;
                    }

                    break;

                case PaymentRelatedEntityType.Donation:
                    var donation = await _dbContext.Donations
                        .Include(d => d.User)
                        .Include(d => d.ZooPartner)
                        .FirstOrDefaultAsync(d => d.PaymentId == payment.Id || d.Id == payment.RelatedEntityId);
                    if (donation?.ZooPartner != null)
                    {
                        var chargedAmount = payment.Amount;
                        await _dbContext.ZooPartners
                            .Where(z => z.Id == donation.ZooPartnerId)
                            .ExecuteUpdateAsync(setters => setters.SetProperty(
                                z => z.CurrentAmount,
                                z => z.CurrentAmount + chargedAmount));

                        var amountEntry = _dbContext.Entry(donation.ZooPartner).Property(z => z.CurrentAmount);
                        amountEntry.CurrentValue = donation.ZooPartner.CurrentAmount + chargedAmount;
                        amountEntry.OriginalValue = amountEntry.CurrentValue;
                        amountEntry.IsModified = false;

                        var donationResponse = _mapper.Map<DonationResponse>(donation);
                        donationResponse.UserFullName = donation.User != null
                            ? $"{donation.User.FirstName} {donation.User.LastName}".Trim()
                            : string.Empty;
                        donationResponse.ZooPartnerName = donation.ZooPartner.Name;
                        completion.Donation = donationResponse;
                    }

                    break;
            }

            return completion;
        }

        private async Task PublishPaidCompletionAsync(Database.Payment payment, PaidCompletion completion)
        {
            var userId = payment.UserId ?? await FindPaymentUserIdAsync(payment.Id);
            if (!userId.HasValue)
            {
                return;
            }

            var sentSpecific = false;
            switch (completion.RelatedType)
            {
                case PaymentRelatedEntityType.Ticket when completion.Ticket != null:
                    await _notificationEvents.NotifyTicketPurchasedAsync(completion.Ticket);
                    sentSpecific = true;
                    break;

                case PaymentRelatedEntityType.Adoption when completion.Adoption != null:
                    await _notificationEvents.NotifyAdoptionCreatedAsync(completion.Adoption);
                    sentSpecific = true;
                    break;

                case PaymentRelatedEntityType.Donation when completion.Donation != null:
                    await _notificationEvents.NotifyDonationReceivedAsync(completion.Donation);
                    sentSpecific = true;
                    break;
            }

            if (!sentSpecific && completion.RelatedType is not (
                PaymentRelatedEntityType.Ticket or PaymentRelatedEntityType.Adoption or PaymentRelatedEntityType.Donation))
            {
                await _notificationEvents.NotifyPaymentCompletedAsync(userId.Value, payment.Id, payment.Amount);
            }

            if (completion.RelatedType is PaymentRelatedEntityType.Reservation
                or PaymentRelatedEntityType.Adoption
                or PaymentRelatedEntityType.Donation)
            {
                await _badgeAwardService.CheckAndAwardBadgesAsync(userId.Value);
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

        private async Task RefundCompletedPaymentAsync(Database.Payment payment, bool persist = true)
        {
            if (payment.Status == PaymentStatus.Refunded)
            {
                return;
            }

            if (payment.Status != PaymentStatus.Completed && payment.Status != PaymentStatus.Failed)
            {
                throw new ClientException("Only a completed payment can be refunded.");
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
                    throw new ClientException("Stripe refund failed. The payment was not marked as refunded.");
                }
            }

            payment.Status = PaymentStatus.Refunded;
            if (persist)
            {
                await _dbContext.SaveChangesAsync();
            }
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
                        await RefundCompletedPaymentAsync(payment, persist: false);
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
                            ThrowNotFound("Ticket", id, "Ticket not found.");
                        }

                        if (ticket.Status is TicketStatus.Cancelled or TicketStatus.Used)
                        {
                            throw new ClientException("This ticket cannot be paid.");
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
                            ThrowNotFound("Reservation", id, "Reservation not found.");
                        }

                        if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed)
                        {
                            throw new ClientException("This reservation cannot be paid.");
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
                            ThrowNotFound("Adoption", id, "Adoption not found.");
                        }

                        if (adoption.Status == AdoptionStatus.Cancelled)
                        {
                            throw new ClientException("This adoption cannot be paid.");
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
                            ThrowNotFound("Donation", id, "Donation not found.");
                        }

                        return new PayableRecord(
                            donation.UserId,
                            donation.Amount,
                            $"Donation #{donation.Id}",
                            donation.PaymentId,
                            payment => donation.PaymentId = payment.Id);
                    }

                default:
                    throw new ClientException("Unsupported related entity type.");
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
                ThrowNotFound("Payment", id, "Payment not found.");
            }

            return entity;
        }

        private async Task RecordProcessedEventAsync(Event stripeEvent)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
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

        private sealed class PaidCompletion
        {
            public PaymentRelatedEntityType? RelatedType { get; set; }
            public TicketResponse? Ticket { get; set; }
            public AdoptionResponse? Adoption { get; set; }
            public DonationResponse? Donation { get; set; }
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
