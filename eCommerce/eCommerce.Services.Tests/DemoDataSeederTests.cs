using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class DemoDataSeederTests
    {
        [Fact]
        public void Plan_StartsToday_AndCoversSeveralSlotsAcrossSixWeeks()
        {
            var today = new DateOnly(2026, 10, 21);
            var plan = DemoDataSeeder.Plan(today);

            Assert.NotEmpty(plan);
            Assert.All(plan, slot => Assert.True(slot.Date >= today));
            Assert.Contains(plan, slot => slot.Date == new DateOnly(2026, 10, 22));
            Assert.DoesNotContain(plan, slot => slot.Date == new DateOnly(2026, 10, 20));

            var last = plan.Max(slot => slot.Date);
            Assert.InRange(last, today.AddDays(28), today.AddDays(42));

            var thisWeek = plan.Count(slot => slot.Date < today.AddDays(7));
            Assert.True(thisWeek >= 3);
            Assert.Contains(plan, slot => slot.Status == ScheduleStatus.Cancelled);
        }

        [Fact]
        public async Task Seed_InsertsFutureSchedulesImagesAndAccounts_WithoutDuplicating()
        {
            using var db = new SqliteDb();
            var today = new DateOnly(2026, 10, 19);

            await DemoDataSeeder.SeedAsync(db.Context, today);
            db.Context.ChangeTracker.Clear();

            var future = await db.Context.Schedules.CountAsync(schedule => schedule.Date >= today);
            var reservations = await db.Context.Reservations.CountAsync();
            var donations = await db.Context.Donations.CountAsync();
            var sunbird = await db.Context.ZooPartners.SingleAsync(partner => partner.Id == 1);
            var mobile = await db.Context.Users.SingleAsync(user => user.Username == "mobile");
            var desktop = await db.Context.Users.SingleAsync(user => user.Username == "desktop");

            Assert.True(future >= 12);
            Assert.True(reservations >= 4);
            Assert.True(donations >= 5);
            Assert.Equal(12425m, sunbird.CurrentAmount);
            Assert.True(new CryptoService().Verify(mobile.PasswordHash, mobile.PasswordSalt, "test"));
            Assert.True(new CryptoService().Verify(desktop.PasswordHash, desktop.PasswordSalt, "test"));
            Assert.Contains(
                await db.Context.Reservations.Select(reservation => reservation.Status).ToListAsync(),
                status => status == ReservationStatus.Pending);
            Assert.Contains(
                await db.Context.Tickets.Select(ticket => ticket.Status).ToListAsync(),
                status => status == TicketStatus.Used);

            await DemoDataSeeder.SeedAsync(db.Context, today);
            db.Context.ChangeTracker.Clear();

            Assert.Equal(future, await db.Context.Schedules.CountAsync(schedule => schedule.Date >= today));
            Assert.Equal(reservations, await db.Context.Reservations.CountAsync());
            Assert.Equal(donations, await db.Context.Donations.CountAsync());
            Assert.Equal(12425m, (await db.Context.ZooPartners.SingleAsync(partner => partner.Id == 1)).CurrentAmount);
            Assert.Equal(1, await db.Context.Users.CountAsync(user => user.Username == "Admin"));
        }

        [Fact]
        public void CoursePassword_DoesNotSatisfyRegistrationRules()
        {
            var validator = new UserInsertValidator();
            var result = validator.Validate(new UserInsertRequest
            {
                FirstName = "Mina",
                LastName = "Cole",
                Email = "mobile@gmail.com",
                Username = "mobile",
                Password = "test",
                Role = "Customer"
            });

            Assert.Contains(result.Errors, error => error.ErrorMessage == "Password must be at least 6 characters.");
        }

        [Fact]
        public void EnsurePresent_CopiesEmbeddedImages_AndSkipsExistingFiles()
        {
            var directory = Path.Combine(Path.GetTempPath(), "wildnest-seed-images-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var written = SeedImages.EnsurePresent(directory);
                Assert.True(written >= 20);
                Assert.True(File.Exists(Path.Combine(directory, "00000000000000000000000000a11001.png")));
                Assert.Equal(0, SeedImages.EnsurePresent(directory));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
