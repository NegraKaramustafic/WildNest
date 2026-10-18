using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Recommendation;
using eCommerce.Services.ReservationStateMachine;
using eCommerce.Services.Validators;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AdminWorkflowTests
    {
        [Fact]
        public async Task Confirm_PaidPendingReservation_RecordsAdminAndPublishesOneStableNotification()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var service = CreateReservations(db.Context, publisher, admin: true, userId: world.AdminId);

            var confirmed = await service.ConfirmAsync(world.PaidReservationId);

            Assert.Equal(ReservationStatus.Confirmed, confirmed.Status);
            Assert.Equal(world.AdminId, confirmed.ConfirmedByUserId);
            Assert.NotNull(confirmed.ConfirmedAt);
            var stored = await db.Context.Reservations.FindAsync(world.PaidReservationId);
            Assert.Equal(ReservationStatus.Confirmed, stored!.Status);
            Assert.Equal(world.AdminId, stored.ConfirmedByUserId);

            var message = Assert.Single(publisher.Messages);
            Assert.Equal(NotificationType.ReservationConfirmed, message.Type);
            Assert.Equal(world.CustomerId, message.UserId);
            Assert.EndsWith(".", message.Message);

            await new NotificationEventService(publisher, db.Context).NotifyReservationConfirmedAsync(confirmed);
            Assert.Equal(2, publisher.Messages.Count);
            Assert.Equal(publisher.Messages[0].MessageId, publisher.Messages[1].MessageId);
        }

        [Fact]
        public async Task Confirm_WithoutCompletedPayment_IsRejected()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var service = CreateReservations(db.Context, publisher, admin: true, userId: world.AdminId);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ConfirmAsync(world.UnpaidReservationId));

            Assert.Equal("Reservation cannot be confirmed until payment is completed.", error.Message);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Reservations.FindAsync(world.UnpaidReservationId);
            Assert.Equal(ReservationStatus.Pending, stored!.Status);
            Assert.Null(stored.ConfirmedByUserId);
            Assert.Empty(publisher.Messages);
        }

        [Fact]
        public async Task Confirm_CompletedReservation_IsRejected()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var service = CreateReservations(db.Context, new CapturingPublisher(), admin: true, userId: world.AdminId);
            var reservation = await db.Context.Reservations.FindAsync(world.PaidReservationId);
            reservation!.Status = ReservationStatus.Completed;
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ConfirmAsync(world.PaidReservationId));

            Assert.Equal("Cannot confirm a reservation in its current state.", error.Message);
        }

        [Fact]
        public async Task CancelWithReason_RestoresCapacity_RequestsRefund_AndNotifiesOnce()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var payments = new RecordingPaymentService();
            var service = CreateReservations(db.Context, publisher, admin: true, userId: world.AdminId, payments: payments);

            var cancelled = await service.CancelWithReasonAsync(
                world.PaidReservationId,
                new ReservationCancelRequest { Reason = "  The activity was full.  " });

            Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
            Assert.Equal("The activity was full.", cancelled.CancellationReason);
            Assert.Equal(world.AdminId, cancelled.CancelledByUserId);
            Assert.NotNull(cancelled.CancelledAt);
            db.Context.ChangeTracker.Clear();
            var schedule = await db.Context.Schedules.FindAsync(world.ScheduleId);
            Assert.Equal(10, schedule!.CapacityLeft);
            var call = Assert.Single(payments.Cancellations);
            Assert.Equal(world.PaidPaymentId, call.PaymentId);
            Assert.True(call.RefundIfCompleted);

            var message = Assert.Single(publisher.Messages);
            Assert.Equal(NotificationType.ReservationCancelled, message.Type);
            Assert.Equal(world.CustomerId, message.UserId);
            Assert.Contains("Reason: The activity was full.", message.Message);
            Assert.EndsWith(".", message.Message);
        }

        [Fact]
        public async Task CancelWithReason_RequiresAReason_AndRejectsCustomers()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var admin = CreateReservations(db.Context, new CapturingPublisher(), admin: true, userId: world.AdminId);
            var customer = CreateReservations(db.Context, new CapturingPublisher(), admin: false, userId: world.CustomerId);

            var missing = await Assert.ThrowsAsync<ClientException>(() =>
                admin.CancelWithReasonAsync(world.UnpaidReservationId, new ReservationCancelRequest { Reason = "   " }));
            Assert.Equal("A reason is required.", missing.Message);

            var tooLong = await Assert.ThrowsAsync<ClientException>(() =>
                admin.CancelWithReasonAsync(world.UnpaidReservationId, new ReservationCancelRequest { Reason = new string('a', 501) }));
            Assert.Equal("Reason must be at most 500 characters.", tooLong.Message);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                customer.CancelWithReasonAsync(world.UnpaidReservationId, new ReservationCancelRequest { Reason = "Changed my mind." }));
        }

        [Fact]
        public async Task CustomerCancel_DoesNotRecordAnAdmin_AndStillReleasesCapacity()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var service = CreateReservations(db.Context, publisher, admin: false, userId: world.CustomerId);

            var cancelled = await service.CancelAsync(world.UnpaidReservationId);

            Assert.Equal(ReservationStatus.Cancelled, cancelled.Status);
            Assert.Null(cancelled.CancelledByUserId);
            Assert.Null(cancelled.CancellationReason);
            db.Context.ChangeTracker.Clear();
            var schedule = await db.Context.Schedules.FindAsync(world.UnpaidScheduleId);
            Assert.Equal(6, schedule!.CapacityLeft);
            var message = Assert.Single(publisher.Messages);
            Assert.DoesNotContain("Reason:", message.Message);
            Assert.EndsWith(".", message.Message);
        }

        [Fact]
        public async Task Cancel_CompletedReservation_IsRejected()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var reservation = await db.Context.Reservations.FindAsync(world.PaidReservationId);
            reservation!.Status = ReservationStatus.Completed;
            await db.Context.SaveChangesAsync();
            var service = CreateReservations(db.Context, new CapturingPublisher(), admin: true, userId: world.AdminId);

            var error = await Assert.ThrowsAsync<ClientException>(() =>
                service.CancelWithReasonAsync(world.PaidReservationId, new ReservationCancelRequest { Reason = "Too late." }));

            Assert.Equal("Cannot cancel a reservation in its current state.", error.Message);
        }

        [Fact]
        public async Task ReservationSearch_FiltersByStatusDateActivityAndText()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var service = CreateReservations(db.Context, new CapturingPublisher(), admin: true, userId: world.AdminId);

            var byStatus = await service.GetAllAsync(new ReservationSearchObject
            {
                Status = ReservationStatus.Pending,
                IncludeTotalCount = true,
                Page = 1,
                PageSize = 10
            });
            Assert.Equal(2, byStatus.TotalCount);

            var byActivity = await service.GetAllAsync(new ReservationSearchObject
            {
                ActivityId = world.ActivityId,
                FTS = "Dave",
                DateFrom = new DateOnly(2026, 4, 2),
                DateTo = new DateOnly(2026, 4, 2)
            });
            var match = Assert.Single(byActivity.Items);
            Assert.Equal(world.PaidReservationId, match.Id);

            var invalid = await Assert.ThrowsAsync<ClientException>(() => service.GetAllAsync(new ReservationSearchObject
            {
                DateFrom = new DateOnly(2026, 5, 1),
                DateTo = new DateOnly(2026, 4, 1)
            }));
            Assert.Equal("From date cannot be after to date.", invalid.Message);
        }

        [Fact]
        public async Task RejectAdoption_RequiresReason_DoesNotRefund_AndNotifiesOnce()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var payments = new RecordingPaymentService();
            var service = CreateAdoptions(db.Context, publisher, payments, admin: true, userId: world.AdminId);

            var missing = await Assert.ThrowsAsync<ClientException>(() =>
                service.RejectAsync(world.ActiveAdoptionId, new AdoptionRejectRequest { Reason = "" }));
            Assert.Equal("A reason is required.", missing.Message);

            var rejected = await service.RejectAsync(
                world.ActiveAdoptionId,
                new AdoptionRejectRequest { Reason = "The animal is not available." });

            Assert.Equal(AdoptionStatus.Cancelled, rejected.Status);
            Assert.Equal(world.AdminId, rejected.RejectedByUserId);
            Assert.Equal("The animal is not available.", rejected.RejectionReason);
            Assert.NotNull(rejected.RejectedAt);
            var call = Assert.Single(payments.Cancellations);
            Assert.False(call.RefundIfCompleted);
            var message = Assert.Single(publisher.Messages);
            Assert.Equal(NotificationType.AdoptionRejected, message.Type);
            Assert.Equal(world.CustomerId, message.UserId);
            Assert.Contains("Kiko", message.Message);
            Assert.Contains("Reason: The animal is not available.", message.Message);
            Assert.EndsWith(".", message.Message);

            await new NotificationEventService(publisher, db.Context).NotifyAdoptionRejectedAsync(rejected);
            Assert.Equal(publisher.Messages[0].MessageId, publisher.Messages[1].MessageId);
        }

        [Fact]
        public async Task ApproveAdoption_DoesNotChangeStatus()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var publisher = new CapturingPublisher();
            var service = CreateAdoptions(db.Context, publisher, new RecordingPaymentService(), admin: true, userId: world.AdminId);

            var active = await Assert.ThrowsAsync<ClientException>(() => service.ApproveAsync(world.ActiveAdoptionId));
            Assert.Equal("This adoption is already active.", active.Message);

            var pending = await Assert.ThrowsAsync<ClientException>(() => service.ApproveAsync(world.PendingAdoptionId));
            Assert.Equal("This adoption cannot be approved by an administrator. It becomes active when payment is completed.", pending.Message);

            Assert.Empty(publisher.Messages);
            db.Context.ChangeTracker.Clear();
            Assert.Equal(AdoptionStatus.Active, (await db.Context.Adoptions.FindAsync(world.ActiveAdoptionId))!.Status);
            Assert.Equal(AdoptionStatus.PendingPayment, (await db.Context.Adoptions.FindAsync(world.PendingAdoptionId))!.Status);
        }

        [Fact]
        public async Task DonationSearch_FiltersPaymentPartnerAndDate_AndUpdateKeepsTheAmount()
        {
            using var db = new SqliteDb();
            var world = await SeedWorldAsync(db.Context);
            var service = CreateDonations(db.Context, admin: true, userId: world.AdminId);

            var paid = await service.GetAllAsync(new DonationSearchObject
            {
                PaymentStatus = PaymentStatus.Completed,
                ZooPartnerId = world.PartnerId,
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                FTS = "River"
            });
            var match = Assert.Single(paid.Items);
            Assert.Equal(world.PaidDonationId, match.Id);
            Assert.Equal(PaymentStatus.Completed, match.PaymentStatus);

            var unpaid = await service.GetAllAsync(new DonationSearchObject { HasPayment = false });
            var open = Assert.Single(unpaid.Items);
            Assert.Equal(world.UnpaidDonationId, open.Id);
            Assert.Null(open.PaymentStatus);

            await service.UpdateAsync(world.PaidDonationId, new DonationUpdateRequest { PaymentId = world.DonationPaymentId });
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Donations.FindAsync(world.PaidDonationId);
            Assert.Equal(25m, stored!.Amount);
        }

        private static ReservationService CreateReservations(
            ECommerceDbContext context,
            CapturingPublisher publisher,
            bool admin,
            int userId,
            RecordingPaymentService? payments = null)
        {
            var loggerFactory = new SilentLoggerFactory();
            var mapper = CreateMapper();
            var accessor = new TestUserAccessor { UserId = userId, Admin = admin };
            var provider = new StateBag();
            var pending = new PendingReservationState(context, mapper, provider, loggerFactory);
            var confirmed = new ConfirmedReservationState(context, mapper, provider, loggerFactory);
            var completed = new CompletedReservationState(context, mapper, provider, loggerFactory);
            var cancelled = new CancelledReservationState(context, mapper, provider, loggerFactory);
            var initial = new InitialReservationState(
                context,
                mapper,
                provider,
                loggerFactory,
                new ReservationInsertValidator(),
                accessor,
                new ZooClock("Europe/Sarajevo"));
            provider.Add(typeof(PendingReservationState), pending);
            provider.Add(typeof(ConfirmedReservationState), confirmed);
            provider.Add(typeof(CompletedReservationState), completed);
            provider.Add(typeof(CancelledReservationState), cancelled);
            provider.Add(typeof(InitialReservationState), initial);
            var root = new BaseReservationState(context, mapper, provider, loggerFactory);

            return new ReservationService(
                context,
                mapper,
                root,
                accessor,
                new NotificationEventService(publisher, context),
                new RecommendationModelCache(),
                payments ?? new RecordingPaymentService(),
                new ReservationCancelValidator(),
                loggerFactory);
        }

        private static AdoptionService CreateAdoptions(
            ECommerceDbContext context,
            CapturingPublisher publisher,
            RecordingPaymentService payments,
            bool admin,
            int userId)
        {
            return new AdoptionService(
                context,
                CreateMapper(),
                new AdoptionInsertValidator(),
                new AdoptionUpdateValidator(),
                new TestUserAccessor { UserId = userId, Admin = admin },
                payments,
                new NotificationEventService(publisher, context),
                new AdoptionRejectValidator(),
                new SilentLoggerFactory());
        }

        private static IMapper CreateMapper()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<Reservation, ReservationResponse>()
                .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
                .Map(dest => dest.ActivityName, src => src.Schedule != null && src.Schedule.Activity != null ? src.Schedule.Activity.Name : string.Empty)
                .Map(dest => dest.ActivityImageUrl, src => src.Schedule != null && src.Schedule.Activity != null ? src.Schedule.Activity.ImageUrl : null)
                .Map(dest => dest.PaymentStatus, src => src.Payment != null ? (PaymentStatus?)src.Payment.Status : null)
                .Map(dest => dest.ConfirmedByFullName, src => src.ConfirmedByUser != null ? $"{src.ConfirmedByUser.FirstName} {src.ConfirmedByUser.LastName}".Trim() : null)
                .Map(dest => dest.CancelledByFullName, src => src.CancelledByUser != null ? $"{src.CancelledByUser.FirstName} {src.CancelledByUser.LastName}".Trim() : null);
            config.NewConfig<Adoption, AdoptionResponse>()
                .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
                .Map(dest => dest.AnimalName, src => src.Animal != null ? src.Animal.Name : string.Empty)
                .Map(dest => dest.AnimalImageUrl, src => src.Animal != null ? src.Animal.ImageUrl : null)
                .Map(dest => dest.PaymentStatus, src => src.Payment != null ? (PaymentStatus?)src.Payment.Status : null)
                .Map(dest => dest.RejectedByFullName, src => src.RejectedByUser != null ? $"{src.RejectedByUser.FirstName} {src.RejectedByUser.LastName}".Trim() : null);
            config.NewConfig<Donation, DonationResponse>()
                .Map(dest => dest.UserFullName, src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : string.Empty)
                .Map(dest => dest.ZooPartnerName, src => src.ZooPartner != null ? src.ZooPartner.Name : string.Empty)
                .Map(dest => dest.ZooPartnerImageUrl, src => src.ZooPartner != null ? src.ZooPartner.ImageUrl : null)
                .Map(dest => dest.PaymentStatus, src => src.Payment != null ? (PaymentStatus?)src.Payment.Status : null);
            return new Mapper(config);
        }

        private static DonationService CreateDonations(ECommerceDbContext context, bool admin, int userId)
        {
            return new DonationService(
                context,
                CreateMapper(),
                new DonationInsertValidator(),
                new DonationUpdateValidator(),
                new TestUserAccessor { UserId = userId, Admin = admin },
                new SilentLoggerFactory());
        }

        private static async Task<World> SeedWorldAsync(ECommerceDbContext context)
        {
            const int customerId = 4;
            const int adminId = 1;

            var activity = new Activity
            {
                Name = "Morning tour",
                ActivityTypeId = 1,
                Price = 15m,
                Capacity = 20,
                Duration = TimeSpan.FromHours(1),
                Status = ActivityStatus.Active,
                ImageUrl = "https://example.com/tour.jpg"
            };
            var otherActivity = new Activity
            {
                Name = "Feeding",
                ActivityTypeId = 1,
                Price = 10m,
                Capacity = 10,
                Duration = TimeSpan.FromMinutes(30),
                Status = ActivityStatus.Active
            };
            context.Activities.AddRange(activity, otherActivity);
            var animal = new Animal
            {
                Name = "Kiko",
                Species = "Lemur",
                CategoryId = 1,
                AdoptionPrice = 12m,
                ImageUrl = "https://example.com/kiko.jpg"
            };
            context.Animals.Add(animal);
            var partner = new ZooPartner
            {
                Name = "River Zoo",
                Location = "Mostar",
                GoalAmount = 1000m,
                ImageUrl = "https://example.com/river.jpg"
            };
            var otherPartner = new ZooPartner
            {
                Name = "Hill Zoo",
                Location = "Blagaj",
                GoalAmount = 500m
            };
            context.ZooPartners.AddRange(partner, otherPartner);
            await context.SaveChangesAsync();

            var schedule = new Schedule
            {
                ActivityId = activity.Id,
                Date = new DateOnly(2026, 4, 2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = 8,
                Status = ScheduleStatus.Active
            };
            var otherSchedule = new Schedule
            {
                ActivityId = otherActivity.Id,
                Date = new DateOnly(2026, 6, 1),
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(11, 30),
                CapacityLeft = 4,
                Status = ScheduleStatus.Active
            };
            context.Schedules.AddRange(schedule, otherSchedule);
            var paidPayment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 30m,
                Status = PaymentStatus.Completed,
                UserId = customerId,
                CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var adoptionPayment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 12m,
                Status = PaymentStatus.Completed,
                UserId = customerId,
                CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var donationPayment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 25m,
                Status = PaymentStatus.Completed,
                UserId = customerId,
                CreatedAt = new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc)
            };
            context.Payments.AddRange(paidPayment, adoptionPayment, donationPayment);
            await context.SaveChangesAsync();

            var paidReservation = new Reservation
            {
                UserId = customerId,
                ScheduleId = schedule.Id,
                GuestCount = 2,
                TotalPrice = 30m,
                Status = ReservationStatus.Pending,
                Code = "WN-PAID01",
                PaymentId = paidPayment.Id,
                CreatedAt = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc)
            };
            var unpaidReservation = new Reservation
            {
                UserId = customerId,
                ScheduleId = otherSchedule.Id,
                GuestCount = 2,
                TotalPrice = 20m,
                Status = ReservationStatus.Pending,
                Code = "WN-OPEN01",
                CreatedAt = new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc)
            };
            context.Reservations.AddRange(paidReservation, unpaidReservation);
            context.Adoptions.AddRange(
                new Adoption
                {
                    UserId = customerId,
                    AnimalId = animal.Id,
                    MonthlyAmount = 12m,
                    Status = AdoptionStatus.Active,
                    StartDate = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc),
                    PaymentId = adoptionPayment.Id
                },
                new Adoption
                {
                    UserId = customerId,
                    AnimalId = animal.Id,
                    MonthlyAmount = 12m,
                    Status = AdoptionStatus.PendingPayment,
                    StartDate = new DateTime(2026, 4, 3, 10, 0, 0, DateTimeKind.Utc)
                });
            var paidDonation = new Donation
            {
                UserId = customerId,
                ZooPartnerId = partner.Id,
                Amount = 25m,
                PaymentId = donationPayment.Id,
                CreatedAt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc)
            };
            var unpaidDonation = new Donation
            {
                UserId = customerId,
                ZooPartnerId = otherPartner.Id,
                Amount = 10m,
                CreatedAt = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc)
            };
            context.Donations.AddRange(paidDonation, unpaidDonation);
            await context.SaveChangesAsync();

            return new World(
                customerId,
                adminId,
                activity.Id,
                schedule.Id,
                otherSchedule.Id,
                paidReservation.Id,
                unpaidReservation.Id,
                paidPayment.Id,
                context.Adoptions.OrderBy(a => a.Id).First(a => a.Status == AdoptionStatus.Active).Id,
                context.Adoptions.OrderBy(a => a.Id).First(a => a.Status == AdoptionStatus.PendingPayment).Id,
                partner.Id,
                paidDonation.Id,
                unpaidDonation.Id,
                donationPayment.Id);
        }

        private sealed record World(
            int CustomerId,
            int AdminId,
            int ActivityId,
            int ScheduleId,
            int UnpaidScheduleId,
            int PaidReservationId,
            int UnpaidReservationId,
            int PaidPaymentId,
            int ActiveAdoptionId,
            int PendingAdoptionId,
            int PartnerId,
            int PaidDonationId,
            int UnpaidDonationId,
            int DonationPaymentId);
    }

    internal sealed class TestUserAccessor : IAuthenticatedUserAccessor
    {
        public int? UserId { get; set; }
        public bool Admin { get; set; }

        public int? GetUserId() => UserId;

        public bool IsInRole(string role) => Admin && role == Roles.Admin;
    }

    internal sealed class RecordingPaymentService : IPaymentService
    {
        public List<(int PaymentId, bool RefundIfCompleted)> Cancellations { get; } = new();

        public Task HandleEntityCancellationAsync(int paymentId, bool refundIfCompleted = true)
        {
            Cancellations.Add((paymentId, refundIfCompleted));
            return Task.CompletedTask;
        }

        public Task<PaymentCheckoutResponse> CreateCheckoutAsync(PaymentCheckoutRequest request) => throw new NotImplementedException();
        public Task HandleWebhookAsync(string json, string? stripeSignature) => throw new NotImplementedException();
        public Task<PaymentResponse> RefundAsync(int id) => throw new NotImplementedException();
        public Task<PageResult<PaymentResponse>> GetAllAsync(PaymentSearchObject? search = null) => throw new NotImplementedException();
        public Task<PaymentResponse> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<PaymentResponse> InsertAsync(PaymentInsertRequest request) => throw new NotImplementedException();
        public Task<PaymentResponse> UpdateAsync(int id, PaymentUpdateRequest request) => throw new NotImplementedException();
        public Task DeleteAsync(int id) => throw new NotImplementedException();
    }

    internal sealed class StateBag : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Add(Type type, object service) => _services[type] = service;

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out var service) ? service : null;
        }
    }

    internal sealed class SilentLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose()
        {
        }
    }
}
