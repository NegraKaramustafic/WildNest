using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Reports;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class PaidRecordIntegrityTests
    {
        private const int CustomerId = 4;

        [Fact]
        public async Task Delete_PaidDonation_KeepsPaymentPartnerTotalReportAndBadge()
        {
            using var db = new SqliteDb();
            var partner = await PartnerAsync(db.Context, "River Fund", current: 80m);
            var createdAt = new DateTime(2026, 4, 5, 9, 0, 0, DateTimeKind.Utc);
            var paymentId = await PaymentAsync(db.Context, 50m, PaymentStatus.Completed);
            var donationId = await DonationAsync(db.Context, partner.Id, 50m, paymentId, createdAt);
            var owner = Donations(db.Context, admin: false, userId: CustomerId);
            var admin = Donations(db.Context, admin: true, userId: 1);

            var ownerError = await Assert.ThrowsAsync<ClientException>(() => owner.DeleteAsync(donationId));
            var adminError = await Assert.ThrowsAsync<ClientException>(() => admin.DeleteAsync(donationId));

            Assert.Equal("A paid donation cannot be deleted.", ownerError.Message);
            Assert.Equal("A paid donation cannot be deleted.", adminError.Message);

            db.Context.ChangeTracker.Clear();
            Assert.True(await db.Context.Donations.AnyAsync(donation => donation.Id == donationId));
            Assert.Equal(PaymentStatus.Completed, (await db.Context.Payments.FindAsync(paymentId))!.Status);
            Assert.Equal(80m, (await db.Context.ZooPartners.FindAsync(partner.Id))!.CurrentAmount);

            var report = await Reports(db.Context).BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ZooPartnerId = partner.Id
            });
            Assert.Equal(50m, report.GrandTotal);
            Assert.Contains(report.Lines, line => line.Amount == 50m && line.PaymentStatus == "Completed");

            var awarded = await Badges(db.Context).CheckAndAwardBadgesAsync(CustomerId);
            Assert.Contains(awarded, badge => badge.BadgeId == 3);
            Assert.True(await db.Context.UserBadges.AnyAsync(row => row.UserId == CustomerId && row.BadgeId == 3));
        }

        [Theory]
        [InlineData(PaymentStatus.Pending)]
        [InlineData(PaymentStatus.Failed)]
        public async Task Delete_OpenDonation_RemovesTheRow(PaymentStatus status)
        {
            using var db = new SqliteDb();
            var partner = await PartnerAsync(db.Context, "Open " + status, current: 10m);
            var paymentId = await PaymentAsync(db.Context, 15m, status);
            var donationId = await DonationAsync(db.Context, partner.Id, 15m, paymentId, DateTime.UtcNow);

            await Donations(db.Context, admin: false, userId: CustomerId).DeleteAsync(donationId);

            db.Context.ChangeTracker.Clear();
            Assert.False(await db.Context.Donations.AnyAsync(donation => donation.Id == donationId));
            Assert.Equal(10m, (await db.Context.ZooPartners.FindAsync(partner.Id))!.CurrentAmount);
        }

        [Fact]
        public async Task Delete_DonationWithoutPayment_RemovesTheRow()
        {
            using var db = new SqliteDb();
            var partner = await PartnerAsync(db.Context, "Unpaid gift", current: 0m);
            var donationId = await DonationAsync(db.Context, partner.Id, 15m, null, DateTime.UtcNow);

            await Donations(db.Context, admin: false, userId: CustomerId).DeleteAsync(donationId);

            Assert.False(await db.Context.Donations.AnyAsync(donation => donation.Id == donationId));
        }

        [Theory]
        [InlineData(PaymentStatus.Completed)]
        [InlineData(PaymentStatus.Refunded)]
        public async Task Delete_PaidAdoption_IsRejected(PaymentStatus status)
        {
            using var db = new SqliteDb();
            var paymentId = await PaymentAsync(db.Context, 15m, status);
            var adoptionId = await AdoptionAsync(db.Context, AdoptionStatus.PendingPayment, paymentId);

            var error = await Assert.ThrowsAsync<ClientException>(() =>
                Adoptions(db.Context, new RecordingPaymentService()).DeleteAsync(adoptionId));

            Assert.Equal("A paid adoption cannot be deleted.", error.Message);
            db.Context.ChangeTracker.Clear();
            Assert.Equal(AdoptionStatus.PendingPayment, (await db.Context.Adoptions.FindAsync(adoptionId))!.Status);
            Assert.Equal(status, (await db.Context.Payments.FindAsync(paymentId))!.Status);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(PaymentStatus.Pending)]
        [InlineData(PaymentStatus.Failed)]
        public async Task Delete_UnpaidPendingAdoption_RemovesTheRow(PaymentStatus? status)
        {
            using var db = new SqliteDb();
            int? paymentId = status.HasValue ? await PaymentAsync(db.Context, 15m, status.Value) : null;
            var adoptionId = await AdoptionAsync(db.Context, AdoptionStatus.PendingPayment, paymentId);

            await Adoptions(db.Context, new RecordingPaymentService()).DeleteAsync(adoptionId);

            Assert.False(await db.Context.Adoptions.AnyAsync(adoption => adoption.Id == adoptionId));
        }

        [Fact]
        public async Task Checkout_CompletedTicketPayment_IsStillRejected()
        {
            using var db = new SqliteDb();
            var paymentId = await PaymentAsync(db.Context, 10m, PaymentStatus.Completed);
            var ticket = new Ticket
            {
                UserId = CustomerId,
                VisitDate = new DateOnly(2026, 12, 1),
                VisitorCount = 1,
                PricePerPerson = 10m,
                TotalPrice = 10m,
                Code = "WN-GUARD1",
                QrCodeData = "WN-GUARD1",
                Status = TicketStatus.Active,
                PaymentId = paymentId,
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Tickets.Add(ticket);
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => Payments(db.Context).CreateCheckoutAsync(new PaymentCheckoutRequest
            {
                RelatedEntityType = PaymentRelatedEntityType.Ticket,
                RelatedEntityId = ticket.Id
            }));

            Assert.Equal("This record already has a completed payment.", error.Message);
            Assert.Equal(1, await db.Context.Payments.CountAsync());
            Assert.Equal(paymentId, (await db.Context.Tickets.FindAsync(ticket.Id))!.PaymentId);
            Assert.Equal(PaymentStatus.Completed, (await db.Context.Payments.FindAsync(paymentId))!.Status);
        }

        [Fact]
        public async Task Cancel_PaidAdoption_EndsItAndLeavesTheRow()
        {
            using var db = new SqliteDb();
            var payments = new RecordingPaymentService();
            var paymentId = await PaymentAsync(db.Context, 15m, PaymentStatus.Completed);
            var adoptionId = await AdoptionAsync(db.Context, AdoptionStatus.Active, paymentId);
            var service = Adoptions(db.Context, payments);

            var cancelled = await service.CancelAsync(adoptionId);

            Assert.Equal(AdoptionStatus.Cancelled, cancelled.Status);
            Assert.Equal((paymentId, false), Assert.Single(payments.Cancellations));
            var stillBlocked = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(adoptionId));
            Assert.Equal("A paid adoption cannot be deleted.", stillBlocked.Message);
            Assert.True(await db.Context.Adoptions.AnyAsync(adoption => adoption.Id == adoptionId && adoption.PaymentId == paymentId));
        }

        [Fact]
        public async Task InactivePartner_RejectsNewDonationAndCheckout_AndKeepsPaidHistory()
        {
            using var db = new SqliteDb();
            var inactive = await PartnerAsync(db.Context, "Closed Sanctuary", current: 40m, active: false);
            var donations = Donations(db.Context, admin: false, userId: CustomerId);
            var before = await db.Context.Donations.CountAsync();

            var missing = await Assert.ThrowsAsync<NotFoundException>(() => donations.InsertAsync(new DonationInsertRequest
            {
                ZooPartnerId = 99999,
                Amount = 10m
            }));
            var blocked = await Assert.ThrowsAsync<ClientException>(() => donations.InsertAsync(new DonationInsertRequest
            {
                ZooPartnerId = inactive.Id,
                Amount = 10m
            }));

            Assert.Equal("Zoo partner not found.", missing.Message);
            Assert.Equal("This zoo partner is not accepting donations.", blocked.Message);
            Assert.Equal(before, await db.Context.Donations.CountAsync());

            var active = await PartnerAsync(db.Context, "Open Sanctuary", current: 40m, active: true);
            var created = await donations.InsertAsync(new DonationInsertRequest
            {
                ZooPartnerId = active.Id,
                Amount = 12m
            });
            Assert.Equal(12m, (await db.Context.Donations.FindAsync(created.Id))!.Amount);

            active.IsActive = false;
            var paidAt = new DateTime(2026, 4, 8, 9, 0, 0, DateTimeKind.Utc);
            var paidPayment = await PaymentAsync(db.Context, 25m, PaymentStatus.Completed);
            await DonationAsync(db.Context, active.Id, 25m, paidPayment, paidAt);
            await db.Context.SaveChangesAsync();

            var paymentsBefore = await db.Context.Payments.CountAsync();
            var checkout = await Assert.ThrowsAsync<ClientException>(() => Payments(db.Context).CreateCheckoutAsync(new PaymentCheckoutRequest
            {
                RelatedEntityType = PaymentRelatedEntityType.Donation,
                RelatedEntityId = created.Id
            }));

            Assert.Equal("This zoo partner is not accepting donations.", checkout.Message);
            Assert.Equal(paymentsBefore, await db.Context.Payments.CountAsync());
            Assert.Null((await db.Context.Donations.FindAsync(created.Id))!.PaymentId);

            var report = await Reports(db.Context).BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ZooPartnerId = active.Id
            });
            Assert.Equal(25m, report.GrandTotal);
            Assert.Equal(40m, (await db.Context.ZooPartners.AsNoTracking().SingleAsync(partner => partner.Id == active.Id)).CurrentAmount);
        }

        [Fact]
        public void DonationBadge_KeepsCriteriaValue2()
        {
            Assert.Equal(2, (int)BadgeCriteria.DonationTotal);
            Assert.Equal(4, (int)BadgeCriteria.FiveAdoptions);
            Assert.Equal(50m, DonationBadgeRule.TotalEuros);
            Assert.Equal("Donate a total of 50 EUR", DonationBadgeRule.Description);
            Assert.DoesNotContain(Enum.GetNames<BadgeCriteria>(), name => name == "ThreeDonations");
        }

        [Fact]
        public async Task DonationBadge_AwardsFromCompletedTotal()
        {
            using var db = new SqliteDb();
            var partner = await PartnerAsync(db.Context, "Badge Fund", current: 0m);
            await GiveAsync(db.Context, 1, partner.Id, 25m, PaymentStatus.Completed);
            await GiveAsync(db.Context, 1, partner.Id, 10m, PaymentStatus.Completed);
            await GiveAsync(db.Context, 1, partner.Id, 5m, PaymentStatus.Completed);
            await GiveAsync(db.Context, 2, partner.Id, 50m, PaymentStatus.Completed);
            await GiveAsync(db.Context, 3, partner.Id, 49.99m, PaymentStatus.Completed);
            await GiveAsync(db.Context, CustomerId, partner.Id, 10m, PaymentStatus.Completed);
            await GiveAsync(db.Context, CustomerId, partner.Id, 100m, PaymentStatus.Refunded);
            await GiveAsync(db.Context, CustomerId, partner.Id, 100m, PaymentStatus.Pending);
            await GiveAsync(db.Context, 5, partner.Id, 30m, PaymentStatus.Completed);
            await GiveAsync(db.Context, 5, partner.Id, 20m, PaymentStatus.Completed);

            var awards = Badges(db.Context);
            Assert.DoesNotContain(await awards.CheckAndAwardBadgesAsync(1), badge => badge.BadgeId == 3);
            Assert.Contains(await awards.CheckAndAwardBadgesAsync(2), badge => badge.BadgeId == 3);
            Assert.DoesNotContain(await awards.CheckAndAwardBadgesAsync(3), badge => badge.BadgeId == 3);
            Assert.DoesNotContain(await awards.CheckAndAwardBadgesAsync(CustomerId), badge => badge.BadgeId == 3);
            Assert.Contains(await awards.CheckAndAwardBadgesAsync(5), badge => badge.BadgeId == 3);
        }

        [Fact]
        public async Task DonationBadge_DoesNotRevokeAnAwardAlreadyStored()
        {
            using var db = new SqliteDb();
            var partner = await PartnerAsync(db.Context, "Kept Badge", current: 0m);
            await GiveAsync(db.Context, CustomerId, partner.Id, 10m, PaymentStatus.Completed);
            var earnedAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
            db.Context.UserBadges.Add(new UserBadge
            {
                UserId = CustomerId,
                BadgeId = 3,
                EarnedAt = earnedAt
            });
            await db.Context.SaveChangesAsync();

            var awarded = await Badges(db.Context).CheckAndAwardBadgesAsync(CustomerId);

            Assert.DoesNotContain(awarded, badge => badge.BadgeId == 3);
            var stored = await db.Context.UserBadges.SingleAsync(row => row.UserId == CustomerId && row.BadgeId == 3);
            Assert.Equal(earnedAt, stored.EarnedAt);
        }

        [Fact]
        public async Task Seed_AwardsDonationBadgeFromCompletedTotal()
        {
            using var db = new SqliteDb();
            await DemoDataSeeder.SeedAsync(db.Context, new DateOnly(2026, 10, 19));
            db.Context.ChangeTracker.Clear();

            var badge = await db.Context.Badges.SingleAsync(item => item.Id == 3);
            Assert.Equal(BadgeCriteria.DonationTotal, badge.Criteria);
            Assert.Equal(DonationBadgeRule.Description, badge.Description);

            Assert.Equal(50m, await CompletedTotalAsync(db.Context, "customer1"));
            Assert.Equal(100m, await CompletedTotalAsync(db.Context, "customer2"));
            Assert.Equal(40m, await CompletedTotalAsync(db.Context, "mobile"));

            var holders = await db.Context.UserBadges
                .Where(row => row.BadgeId == 3)
                .Select(row => row.User.Username)
                .ToListAsync();
            Assert.Contains("customer1", holders);
            Assert.Contains("customer2", holders);
            Assert.DoesNotContain("mobile", holders);
        }

        [Fact]
        public async Task Ticket_RejectsAPastDateAndAVisitorCountOutside1To20()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var service = Tickets(db.Context, clock);

            var past = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new TicketInsertRequest
            {
                VisitDate = clock.Today.AddDays(-1),
                VisitorCount = 2
            }));
            var none = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new TicketInsertRequest
            {
                VisitDate = clock.Today,
                VisitorCount = 0
            }));
            var tooMany = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new TicketInsertRequest
            {
                VisitDate = clock.Today,
                VisitorCount = 21
            }));

            Assert.Contains(past.Errors, error => error.ErrorMessage == TicketLimits.VisitDatePastMessage);
            Assert.Contains(none.Errors, error => error.ErrorMessage == TicketLimits.VisitorCountMessage);
            Assert.Contains(tooMany.Errors, error => error.ErrorMessage == TicketLimits.VisitorCountMessage);
            Assert.Empty(await db.Context.Tickets.ToListAsync());

            var created = await service.InsertAsync(new TicketInsertRequest
            {
                VisitDate = clock.Today,
                VisitorCount = TicketLimits.MaximumVisitors
            });
            Assert.Equal(ZooPricing.ZooEntryPricePerPerson * TicketLimits.MaximumVisitors, created.TotalPrice);

            var pastUpdate = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, new TicketUpdateRequest
            {
                VisitDate = clock.Today.AddDays(-1),
                VisitorCount = 2,
                PricePerPerson = 1m,
                Status = TicketStatus.Active
            }));
            var countUpdate = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(created.Id, new TicketUpdateRequest
            {
                VisitDate = clock.Today.AddDays(1),
                VisitorCount = 21,
                PricePerPerson = 1m,
                Status = TicketStatus.Active
            }));

            Assert.Contains(pastUpdate.Errors, error => error.ErrorMessage == TicketLimits.VisitDatePastMessage);
            Assert.Contains(countUpdate.Errors, error => error.ErrorMessage == TicketLimits.VisitorCountMessage);
            var stored = await db.Context.Tickets.AsNoTracking().SingleAsync(ticket => ticket.Id == created.Id);
            Assert.Equal(clock.Today, stored.VisitDate);
            Assert.Equal(TicketLimits.MaximumVisitors, stored.VisitorCount);
            Assert.Equal(created.TotalPrice, stored.TotalPrice);
            Assert.Equal(created.Code, stored.Code);
        }

        [Fact]
        public async Task Ticket_RejectsVisitChangesWhenPaid_AndAllowsThemWhileAwaitingPayment()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var service = Tickets(db.Context, clock);
            var visit = clock.Today.AddDays(4);
            var created = await service.InsertAsync(new TicketInsertRequest
            {
                VisitDate = visit,
                VisitorCount = 2
            });
            var ticket = await db.Context.Tickets.SingleAsync(item => item.Id == created.Id);
            var originalCode = ticket.Code;
            var originalTotal = ticket.TotalPrice;
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = originalTotal,
                Status = PaymentStatus.Pending,
                UserId = CustomerId,
                CreatedAt = DateTime.UtcNow
            };
            db.Context.Payments.Add(payment);
            await db.Context.SaveChangesAsync();
            ticket.PaymentId = payment.Id;
            await db.Context.SaveChangesAsync();

            var updated = await service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = visit.AddDays(1),
                VisitorCount = 4,
                PricePerPerson = 999m,
                Status = TicketStatus.Cancelled
            });

            Assert.Equal(visit.AddDays(1), updated.VisitDate);
            Assert.Equal(4, updated.VisitorCount);
            Assert.Equal(ZooPricing.ZooEntryPricePerPerson * 4, updated.TotalPrice);
            Assert.Equal(originalCode, updated.Code);
            Assert.Equal(TicketStatus.PendingPayment, updated.Status);
            Assert.Equal(ZooPricing.ZooEntryPricePerPerson, updated.PricePerPerson);
            Assert.Equal(originalTotal, (await db.Context.Payments.FindAsync(payment.Id))!.Amount);

            payment.Status = PaymentStatus.Completed;
            ticket.Status = TicketStatus.PendingPayment;
            await db.Context.SaveChangesAsync();
            var paid = await Assert.ThrowsAsync<ClientException>(() => service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = visit.AddDays(3),
                VisitorCount = 4,
                PricePerPerson = 1m,
                Status = TicketStatus.Active
            }));
            Assert.Equal("Visit date and visitor count cannot be changed on a paid ticket.", paid.Message);

            payment.Status = PaymentStatus.Refunded;
            await db.Context.SaveChangesAsync();
            var refunded = await Assert.ThrowsAsync<ClientException>(() => service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = visit.AddDays(1),
                VisitorCount = 5,
                PricePerPerson = 1m,
                Status = TicketStatus.Active
            }));
            Assert.Equal("Visit date and visitor count cannot be changed on a paid ticket.", refunded.Message);

            payment.Status = PaymentStatus.Pending;
            ticket.Status = TicketStatus.Active;
            await db.Context.SaveChangesAsync();
            var active = await Assert.ThrowsAsync<ClientException>(() => service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = visit.AddDays(2),
                VisitorCount = 4,
                PricePerPerson = 1m,
                Status = TicketStatus.PendingPayment
            }));
            Assert.Equal("Visit date and visitor count cannot be changed on a paid ticket.", active.Message);

            db.Context.ChangeTracker.Clear();
            var unchanged = await db.Context.Tickets.AsNoTracking().SingleAsync(item => item.Id == ticket.Id);
            Assert.Equal(visit.AddDays(1), unchanged.VisitDate);
            Assert.Equal(4, unchanged.VisitorCount);
            Assert.Equal(ZooPricing.ZooEntryPricePerPerson * 4, unchanged.TotalPrice);
            Assert.Equal(originalCode, unchanged.Code);
            Assert.Equal(TicketStatus.Active, unchanged.Status);
            Assert.Equal(ZooPricing.ZooEntryPricePerPerson, unchanged.PricePerPerson);
            Assert.Equal(payment.Id, unchanged.PaymentId);
            Assert.Equal(originalTotal, (await db.Context.Payments.FindAsync(payment.Id))!.Amount);

            var sameValues = await service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = unchanged.VisitDate,
                VisitorCount = unchanged.VisitorCount,
                PricePerPerson = 999m,
                Status = TicketStatus.Cancelled
            });
            Assert.Equal(unchanged.VisitDate, sameValues.VisitDate);
            Assert.Equal(unchanged.VisitorCount, sameValues.VisitorCount);
            Assert.Equal(unchanged.TotalPrice, sameValues.TotalPrice);
            Assert.Equal(TicketStatus.Active, sameValues.Status);
            Assert.Equal(originalCode, sameValues.Code);

            var reloaded = await db.Context.Tickets.SingleAsync(item => item.Id == ticket.Id);
            reloaded.Status = TicketStatus.Used;
            await db.Context.SaveChangesAsync();
            var used = await Assert.ThrowsAsync<ClientException>(() => service.UpdateAsync(ticket.Id, new TicketUpdateRequest
            {
                VisitDate = visit.AddDays(6),
                VisitorCount = 1,
                PricePerPerson = 1m,
                Status = TicketStatus.Active
            }));
            Assert.Equal("A used or cancelled ticket cannot be edited.", used.Message);
            Assert.Equal(sameValues.VisitDate, (await db.Context.Tickets.AsNoTracking().SingleAsync(item => item.Id == ticket.Id)).VisitDate);
        }

        private static async Task<decimal> CompletedTotalAsync(ECommerceDbContext context, string username)
        {
            var userId = await context.Users.Where(user => user.Username == username).Select(user => user.Id).SingleAsync();
            return await context.Donations
                .Where(donation => donation.UserId == userId && donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed)
                .SumAsync(donation => (decimal?)donation.Amount) ?? 0m;
        }

        private static async Task GiveAsync(
            ECommerceDbContext context,
            int userId,
            int partnerId,
            decimal amount,
            PaymentStatus status)
        {
            var paymentId = await PaymentAsync(context, amount, status, userId);
            await DonationAsync(context, partnerId, amount, paymentId, DateTime.UtcNow, userId);
        }

        private static async Task<ZooPartner> PartnerAsync(
            ECommerceDbContext context,
            string name,
            decimal current,
            bool active = true)
        {
            var partner = new ZooPartner
            {
                Name = name,
                Location = "Sarajevo",
                GoalAmount = 1000m,
                CurrentAmount = current,
                IsActive = active
            };
            context.ZooPartners.Add(partner);
            await context.SaveChangesAsync();
            return partner;
        }

        private static async Task<int> PaymentAsync(
            ECommerceDbContext context,
            decimal amount,
            PaymentStatus status,
            int userId = CustomerId)
        {
            var payment = new Payment
            {
                Method = PaymentMethod.Card,
                Amount = amount,
                Status = status,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            return payment.Id;
        }

        private static async Task<int> DonationAsync(
            ECommerceDbContext context,
            int partnerId,
            decimal amount,
            int? paymentId,
            DateTime createdAt,
            int userId = CustomerId)
        {
            var donation = new Donation
            {
                UserId = userId,
                ZooPartnerId = partnerId,
                Amount = amount,
                PaymentId = paymentId,
                CreatedAt = createdAt
            };
            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            return donation.Id;
        }

        private static async Task<int> AdoptionAsync(ECommerceDbContext context, AdoptionStatus status, int? paymentId)
        {
            var adoption = new Adoption
            {
                UserId = CustomerId,
                AnimalId = 1,
                MonthlyAmount = 15m,
                Status = status,
                StartDate = DateTime.UtcNow,
                PaymentId = paymentId
            };
            context.Adoptions.Add(adoption);
            await context.SaveChangesAsync();
            return adoption.Id;
        }

        private static DonationService Donations(ECommerceDbContext context, bool admin, int userId)
        {
            return new DonationService(
                context,
                new Mapper(),
                new DonationInsertValidator(),
                new DonationUpdateValidator(),
                new TestUserAccessor { UserId = userId, Admin = admin },
                new SilentLoggerFactory());
        }

        private static AdoptionService Adoptions(ECommerceDbContext context, RecordingPaymentService payments)
        {
            return new AdoptionService(
                context,
                new Mapper(),
                new AdoptionInsertValidator(),
                new AdoptionUpdateValidator(),
                new TestUserAccessor { UserId = CustomerId, Admin = false },
                payments,
                new NotificationEventService(new CapturingPublisher(), context),
                new AdoptionRejectValidator(),
                new AdoptionTermService(context, new ZooClock("Europe/Sarajevo")),
                new SilentLoggerFactory());
        }

        private static TicketService Tickets(ECommerceDbContext context, ZooClock clock)
        {
            return new TicketService(
                context,
                new Mapper(),
                new TicketInsertValidator(clock),
                new TicketUpdateValidator(clock),
                new TestUserAccessor { UserId = CustomerId, Admin = false },
                new NotificationEventService(new CapturingPublisher(), context),
                new RecordingPaymentService(),
                clock,
                new IdleReservationService(),
                new SilentLoggerFactory());
        }

        private static PaymentService Payments(ECommerceDbContext context)
        {
            SetIfMissing("STRIPE_SECRET_KEY", "sk_test_placeholder");
            SetIfMissing("STRIPE_WEBHOOK_SECRET", "whsec_placeholder");
            SetIfMissing("STRIPE_CURRENCY", "eur");
            SetIfMissing("STRIPE_SUCCESS_URL", "wildnest://payment-success");
            SetIfMissing("STRIPE_CANCEL_URL", "wildnest://payment-cancel");
            return new PaymentService(
                context,
                new Mapper(),
                new PaymentInsertValidator(),
                new PaymentUpdateValidator(),
                new PaymentCheckoutValidator(),
                new TestUserAccessor { UserId = CustomerId, Admin = false },
                new NotificationEventService(new CapturingPublisher(), context),
                new NoBadges(),
                StripeSettings.FromEnvironment(),
                new AdoptionTermService(context, new ZooClock("Europe/Sarajevo")),
                new SilentLoggerFactory());
        }

        private static ReportService Reports(ECommerceDbContext context)
        {
            return new ReportService(
                context,
                new TestUserAccessor { UserId = 1, Admin = true },
                new ReservationReportValidator(),
                new DonationReportValidator(),
                new QuestPdfReportRenderer(),
                NullLogger<ReportService>.Instance);
        }

        private static BadgeAwardService Badges(ECommerceDbContext context)
        {
            return new BadgeAwardService(context, new NotificationEventService(new CapturingPublisher(), context));
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
    }
}
