using eCommerce.Model.Enums;
using eCommerce.Services;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Validators;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class PaymentIntegrityTests
    {
        [Fact]
        public async Task DonationRefund_SubtractsChargedAmountOnce_AndClampsAtZero()
        {
            using var db = new SqliteDb();
            var service = Payments(db.Context);
            var partnerId = await Partner(db.Context, 20m);
            var paymentId = await CompletedDonation(db.Context, partnerId, 10m);

            await service.RefundAsync(paymentId);
            await service.RefundAsync(paymentId);

            db.Context.ChangeTracker.Clear();
            var amount = await db.Context.ZooPartners.Where(z => z.Id == partnerId).Select(z => z.CurrentAmount).SingleAsync();
            var status = await db.Context.Payments.Where(p => p.Id == paymentId).Select(p => p.Status).SingleAsync();
            Assert.Equal(10m, amount);
            Assert.Equal(PaymentStatus.Refunded, status);

            var smallPartner = await Partner(db.Context, 4m);
            var largeRefund = await CompletedDonation(db.Context, smallPartner, 10m);
            await service.RefundAsync(largeRefund);
            db.Context.ChangeTracker.Clear();
            var clamped = await db.Context.ZooPartners.Where(z => z.Id == smallPartner).Select(z => z.CurrentAmount).SingleAsync();
            Assert.Equal(0m, clamped);
        }

        [Fact]
        public async Task MarkFailed_DoesNotOverwriteACompletedPayment()
        {
            using var db = new SqliteDb();
            var service = Payments(db.Context);
            var pending = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 5m,
                Status = PaymentStatus.Pending,
                UserId = 4,
                CreatedAt = DateTime.UtcNow
            };
            var completed = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = 5m,
                Status = PaymentStatus.Completed,
                UserId = 4,
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Payments.AddRange(pending, completed);
            await db.Context.SaveChangesAsync();

            Assert.True(await service.TryMarkFailedIfPendingAsync(pending));
            Assert.False(await service.TryMarkFailedIfPendingAsync(completed));

            db.Context.ChangeTracker.Clear();
            Assert.Equal(PaymentStatus.Failed, (await db.Context.Payments.FindAsync(pending.Id))!.Status);
            Assert.Equal(PaymentStatus.Completed, (await db.Context.Payments.FindAsync(completed.Id))!.Status);
        }

        [Fact]
        public async Task Verify_ParallelScans_OnlyOneMarksTheTicketUsed()
        {
            using var shared = new SharedSqlite();
            string code;
            using (var seed = shared.CreateContext())
            {
                var clock = new ZooClock("Europe/Sarajevo");
                var payment = new Payment
                {
                    Method = PaymentMethod.Card,
                    Amount = 10m,
                    Status = PaymentStatus.Completed,
                    UserId = 4,
                    RelatedEntityType = "Ticket",
                    CreatedAt = DateTime.UtcNow
                };
                seed.Payments.Add(payment);
                await seed.SaveChangesAsync();
                var ticket = new Ticket
                {
                    UserId = 4,
                    VisitDate = clock.Today,
                    VisitorCount = 1,
                    PricePerPerson = 10m,
                    TotalPrice = 10m,
                    Code = "WN-SCAN1",
                    QrCodeData = "WN-SCAN1",
                    Status = TicketStatus.Active,
                    PaymentId = payment.Id,
                    CreatedAt = DateTime.UtcNow
                };
                seed.Tickets.Add(ticket);
                await seed.SaveChangesAsync();
                code = ticket.Code;
            }

            var first = Task.Run(() => Scan(shared, code));
            var second = Task.Run(() => Scan(shared, code));
            var results = await Task.WhenAll(Wrap(first), Wrap(second));

            Assert.Equal(1, results.Count(result => result == null));
            Assert.Contains("Ticket already used.", results);

            using var check = shared.CreateContext();
            var stored = await check.Tickets.SingleAsync(t => t.Code == code);
            Assert.Equal(TicketStatus.Used, stored.Status);
            Assert.NotNull(stored.VerifiedAt);
            Assert.Equal(1, stored.VerifiedByUserId);
        }

        private static async Task<string?> Wrap(Task task)
        {
            try
            {
                await task;
                return null;
            }
            catch (ClientException ex)
            {
                return ex.Message;
            }
        }

        private static async Task Scan(SharedSqlite shared, string code)
        {
            using var context = shared.CreateContext();
            var service = Tickets(context);
            await service.VerifyAsync(new TicketVerifyRequest
            {
                Code = code,
                Method = TicketVerificationMethod.QrScan
            });
        }

        private static async Task<int> Partner(ECommerceDbContext context, decimal current)
        {
            var partner = new ZooPartner
            {
                Name = "Refund " + Guid.NewGuid().ToString("N")[..8],
                Location = "Sarajevo",
                GoalAmount = 100m,
                CurrentAmount = current,
                IsActive = true
            };
            context.ZooPartners.Add(partner);
            await context.SaveChangesAsync();
            return partner.Id;
        }

        private static async Task<int> CompletedDonation(ECommerceDbContext context, int partnerId, decimal amount)
        {
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = amount,
                Status = PaymentStatus.Completed,
                UserId = 4,
                RelatedEntityType = nameof(PaymentRelatedEntityType.Donation),
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            var donation = new Donation
            {
                UserId = 4,
                ZooPartnerId = partnerId,
                Amount = amount,
                PaymentId = payment.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            payment.RelatedEntityId = donation.Id;
            await context.SaveChangesAsync();
            return payment.Id;
        }

        private static PaymentService Payments(ECommerceDbContext context)
        {
            EnsureStripeEnvironment();
            return new PaymentService(
                context,
                new Mapper(),
                new PaymentInsertValidator(),
                new PaymentUpdateValidator(),
                new PaymentCheckoutValidator(),
                new TestUserAccessor { UserId = 1, Admin = true },
                new NotificationEventService(new CapturingPublisher(), context),
                new NoBadges(),
                StripeSettings.FromEnvironment(),
                new SilentLoggerFactory());
        }

        private static TicketService Tickets(ECommerceDbContext context)
        {
            return new TicketService(
                context,
                new Mapper(),
                new TicketInsertValidator(),
                new TicketUpdateValidator(),
                new TestUserAccessor { UserId = 1, Admin = true },
                new NotificationEventService(new CapturingPublisher(), context),
                new RecordingPaymentService(),
                new ZooClock("Europe/Sarajevo"),
                new SilentLoggerFactory());
        }

        private static void EnsureStripeEnvironment()
        {
            SetIfMissing("STRIPE_SECRET_KEY", "sk_test_placeholder");
            SetIfMissing("STRIPE_WEBHOOK_SECRET", "whsec_placeholder");
            SetIfMissing("STRIPE_CURRENCY", "eur");
            SetIfMissing("STRIPE_SUCCESS_URL", "wildnest://payment-success");
            SetIfMissing("STRIPE_CANCEL_URL", "wildnest://payment-cancel");
        }

        private static void SetIfMissing(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        private sealed class NoBadges : IBadgeAwardService
        {
            public Task<IReadOnlyList<AwardedBadge>> CheckAndAwardBadgesAsync(int userId)
            {
                return Task.FromResult<IReadOnlyList<AwardedBadge>>(Array.Empty<AwardedBadge>());
            }
        }

        private sealed class CapturingPublisher : INotificationPublisher
        {
            public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
