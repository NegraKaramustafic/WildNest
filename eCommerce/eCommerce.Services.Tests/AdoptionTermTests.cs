using eCommerce.Model.Enums;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AdoptionTermTests
    {
        [Fact]
        public void FirstPayment_SetsActiveForOneCalendarMonth()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var adoption = new Adoption
            {
                UserId = 4,
                AnimalId = 1,
                MonthlyAmount = 15m,
                Status = AdoptionStatus.PendingPayment,
                StartDate = DateTime.UtcNow
            };

            new AdoptionTermService(db.Context, clock).ApplyCompletedPayment(adoption);

            Assert.Equal(AdoptionStatus.Active, adoption.Status);
            Assert.Equal(clock.Today.AddMonths(1), adoption.ValidUntil);
        }

        [Fact]
        public void Extension_BeforeTheEndDate_AddsOneCalendarMonth()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var currentEnd = clock.Today.AddDays(10);
            var adoption = new Adoption
            {
                UserId = 4,
                AnimalId = 1,
                MonthlyAmount = 15m,
                Status = AdoptionStatus.Active,
                ValidUntil = currentEnd,
                StartDate = DateTime.UtcNow
            };

            new AdoptionTermService(db.Context, clock).ApplyCompletedPayment(adoption);

            Assert.Equal(AdoptionStatus.Active, adoption.Status);
            Assert.Equal(currentEnd.AddMonths(1), adoption.ValidUntil);
        }

        [Fact]
        public void Renewal_AfterExpiry_StartsAMonthFromTodayAndActivates()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            var service = new AdoptionTermService(db.Context, clock);
            var stillMarkedActive = new Adoption
            {
                UserId = 4,
                AnimalId = 1,
                MonthlyAmount = 15m,
                Status = AdoptionStatus.Active,
                ValidUntil = clock.Today.AddDays(-1),
                StartDate = DateTime.UtcNow
            };
            var alreadyExpired = new Adoption
            {
                UserId = 4,
                AnimalId = 1,
                MonthlyAmount = 15m,
                Status = AdoptionStatus.Expired,
                ValidUntil = clock.Today.AddMonths(-2),
                StartDate = DateTime.UtcNow
            };

            service.ApplyCompletedPayment(stillMarkedActive);
            service.ApplyCompletedPayment(alreadyExpired);

            Assert.Equal(AdoptionStatus.Active, stillMarkedActive.Status);
            Assert.Equal(clock.Today.AddMonths(1), stillMarkedActive.ValidUntil);
            Assert.Equal(AdoptionStatus.Active, alreadyExpired.Status);
            Assert.Equal(clock.Today.AddMonths(1), alreadyExpired.ValidUntil);
            Assert.False(service.IsCurrentlyActive(AdoptionStatus.Active, clock.Today.AddDays(-1)));
            Assert.True(service.IsCurrentlyActive(AdoptionStatus.Active, clock.Today));
        }

        [Fact]
        public async Task ExpireElapsed_MarksOnlyPastActiveAdoptions()
        {
            using var db = new SqliteDb();
            var clock = new ZooClock("Europe/Sarajevo");
            db.Context.Adoptions.AddRange(
                Adoption(AdoptionStatus.Active, clock.Today.AddDays(-1)),
                Adoption(AdoptionStatus.Active, clock.Today),
                Adoption(AdoptionStatus.Active, null),
                Adoption(AdoptionStatus.PendingPayment, null),
                Adoption(AdoptionStatus.Cancelled, clock.Today.AddDays(-3)));
            await db.Context.SaveChangesAsync();

            var expired = await new AdoptionTermService(db.Context, clock).ExpireElapsedAsync();

            db.Context.ChangeTracker.Clear();
            Assert.Equal(1, expired);
            var rows = await db.Context.Adoptions.OrderBy(item => item.Id).ToListAsync();
            Assert.Equal(AdoptionStatus.Expired, rows[0].Status);
            Assert.Equal(AdoptionStatus.Active, rows[1].Status);
            Assert.Equal(AdoptionStatus.Active, rows[2].Status);
            Assert.Equal(AdoptionStatus.PendingPayment, rows[3].Status);
            Assert.Equal(AdoptionStatus.Cancelled, rows[4].Status);
        }

        private static Adoption Adoption(AdoptionStatus status, DateOnly? validUntil)
        {
            return new Adoption
            {
                UserId = 4,
                AnimalId = 1,
                MonthlyAmount = 12m,
                Status = status,
                ValidUntil = validUntil,
                StartDate = DateTime.UtcNow
            };
        }
    }
}
