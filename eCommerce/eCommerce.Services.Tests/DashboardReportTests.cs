using System.Globalization;
using System.Reflection;
using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Reports;
using eCommerce.Services.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class DashboardReportTests
    {
        [Fact]
        public async Task Dashboard_AggregatesCompletedRevenue_AndExcludesOldOrUnpaidRows()
        {
            using var db = new SqliteDb();
            var now = DateTime.UtcNow;
            var today = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0, DateTimeKind.Utc);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var previousMonth = monthStart.AddDays(-1).AddHours(12);
            var outsideWindow = monthStart.AddMonths(-12);

            var activeBefore = await db.Context.Schedules.CountAsync(schedule => schedule.Status == ScheduleStatus.Active);
            var usersBefore = await db.Context.Users.CountAsync(user =>
                user.CreatedAt >= monthStart && user.CreatedAt < monthStart.AddMonths(1));

            db.Context.Schedules.Add(new Schedule
            {
                ActivityId = 1,
                Date = new DateOnly(2026, 11, 1),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = 4,
                Status = ScheduleStatus.Active
            });
            db.Context.Schedules.Add(new Schedule
            {
                ActivityId = 2,
                Date = new DateOnly(2026, 11, 2),
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(10, 0),
                CapacityLeft = 4,
                Status = ScheduleStatus.Cancelled
            });
            db.Context.Users.Add(new User
            {
                FirstName = "Nina",
                LastName = "New",
                Email = "nina-new@example.com",
                Username = "nina-new",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                SecurityStamp = "stamp-dashboard-report-0001",
                IsActive = true,
                CreatedAt = today
            });

            var todayTicketPayment = Payment(15m, PaymentStatus.Completed, today);
            var pendingTicketPayment = Payment(99m, PaymentStatus.Pending, today);
            var previousTicketPayment = Payment(7m, PaymentStatus.Completed, previousMonth);
            var refundedTicketPayment = Payment(50m, PaymentStatus.Refunded, today);
            var todayReservationPayment = Payment(40m, PaymentStatus.Completed, today);
            var monthReservationPayment = Payment(12m, PaymentStatus.Completed, monthStart.AddHours(1));
            var previousReservationPayment = Payment(10m, PaymentStatus.Completed, previousMonth);
            var oldReservationPayment = Payment(9m, PaymentStatus.Completed, outsideWindow);
            var cancelledReservationPayment = Payment(30m, PaymentStatus.Refunded, today);
            var todayDonationPayment = Payment(20m, PaymentStatus.Completed, today);
            var pendingDonationPayment = Payment(3m, PaymentStatus.Pending, today);
            var failedDonationPayment = Payment(4m, PaymentStatus.Failed, today);
            var previousDonationPayment = Payment(8m, PaymentStatus.Completed, previousMonth);
            var oldDonationPayment = Payment(100m, PaymentStatus.Completed, outsideWindow);
            var activeAdoptionPayment = Payment(11m, PaymentStatus.Completed, today);
            db.Context.Payments.AddRange(
                todayTicketPayment,
                pendingTicketPayment,
                previousTicketPayment,
                refundedTicketPayment,
                todayReservationPayment,
                monthReservationPayment,
                previousReservationPayment,
                oldReservationPayment,
                cancelledReservationPayment,
                todayDonationPayment,
                pendingDonationPayment,
                failedDonationPayment,
                previousDonationPayment,
                oldDonationPayment,
                activeAdoptionPayment);
            await db.Context.SaveChangesAsync();

            var activityOne = await ScheduleFor(db.Context, 1);
            var activityTwo = await ScheduleFor(db.Context, 2);

            db.Context.Tickets.AddRange(
                Ticket("TCK-TODAY", 15m, today, todayTicketPayment.Id, TicketStatus.Active),
                Ticket("TCK-PEND", 99m, today, pendingTicketPayment.Id, TicketStatus.PendingPayment),
                Ticket("TCK-PREV", 7m, previousMonth, previousTicketPayment.Id, TicketStatus.Active),
                Ticket("TCK-REF", 50m, today, refundedTicketPayment.Id, TicketStatus.Cancelled));

            db.Context.Reservations.AddRange(
                Reservation("WN-TODAY", activityOne, 2, 40m, ReservationStatus.Confirmed, today, todayReservationPayment.Id),
                Reservation("WN-MONTH", activityOne, 1, 12m, ReservationStatus.Pending, monthStart.AddHours(1), monthReservationPayment.Id),
                Reservation("WN-PREV", activityTwo, 1, 10m, ReservationStatus.Confirmed, previousMonth, previousReservationPayment.Id),
                Reservation("WN-OLD", activityOne, 1, 9m, ReservationStatus.Confirmed, outsideWindow, oldReservationPayment.Id),
                Reservation("WN-CANC", activityTwo, 3, 30m, ReservationStatus.Cancelled, today, cancelledReservationPayment.Id));

            db.Context.Donations.AddRange(
                Donation(1, 20m, today, todayDonationPayment.Id),
                Donation(1, 3m, today, pendingDonationPayment.Id),
                Donation(2, 4m, today, failedDonationPayment.Id),
                Donation(2, 5m, today, null),
                Donation(1, 8m, previousMonth, previousDonationPayment.Id),
                Donation(2, 100m, outsideWindow, oldDonationPayment.Id));

            var animals = await db.Context.Animals.AsNoTracking()
                .OrderBy(animal => animal.Id)
                .Take(2)
                .Select(animal => new { animal.Id, animal.Name, Category = animal.Category.Name })
                .ToListAsync();
            db.Context.Adoptions.AddRange(
                new Adoption
                {
                    UserId = 4,
                    AnimalId = animals[0].Id,
                    MonthlyAmount = 11m,
                    Status = AdoptionStatus.Active,
                    StartDate = today,
                    PaymentId = activeAdoptionPayment.Id
                },
                new Adoption
                {
                    UserId = 4,
                    AnimalId = animals[1].Id,
                    MonthlyAmount = 9m,
                    Status = AdoptionStatus.PendingPayment,
                    StartDate = today
                });
            await db.Context.SaveChangesAsync();

            var dashboard = await Dashboard(db.Context, admin: true, userId: 1).GetAsync();

            Assert.Equal(today.Day == 1 ? 3 : 2, dashboard.ReservationsToday);
            Assert.Equal(3, dashboard.ReservationsThisMonth);
            Assert.Equal(15m, dashboard.TicketRevenueThisMonth);
            Assert.Equal(20m, dashboard.DonationRevenueThisMonth);
            Assert.Equal(35m, dashboard.RevenueThisMonth);
            Assert.Equal(activeBefore + 1, dashboard.ActiveSchedules);
            Assert.Equal(usersBefore + 1, dashboard.NewUsersThisMonth);

            var rawAverage = await db.Context.Reviews.AverageAsync(review => (double)review.Rating);
            Assert.Equal(Math.Round(rawAverage, 1, MidpointRounding.AwayFromZero), dashboard.AverageRating);

            Assert.Equal(12, dashboard.Months.Count);
            var current = dashboard.Months.Single(point => point.Year == today.Year && point.Month == today.Month);
            Assert.Equal(3, current.ReservationCount);
            Assert.Equal(35m, current.Revenue);

            var previous = dashboard.Months.Single(point => point.Year == previousMonth.Year && point.Month == previousMonth.Month);
            Assert.Equal(1, previous.ReservationCount);
            Assert.Equal(15m, previous.Revenue);
            Assert.DoesNotContain(dashboard.Months, point => point.Year == outsideWindow.Year && point.Month == outsideWindow.Month && point.ReservationCount > 0);

            Assert.Equal(2, dashboard.TopActivities.Count);
            Assert.Equal("Elephant Feeding Experience", dashboard.TopActivities[0].ActivityName);
            Assert.Equal(2, dashboard.TopActivities[0].ReservationCount);
            Assert.Equal(52m, dashboard.TopActivities[0].Revenue);
            Assert.Equal("Nocturnal Safari Walk", dashboard.TopActivities[1].ActivityName);
            Assert.Equal(1, dashboard.TopActivities[1].ReservationCount);
            Assert.Equal(10m, dashboard.TopActivities[1].Revenue);

            Assert.Equal(
                new[] { "Pending", "Completed", "Failed", "No payment" },
                dashboard.DonationsByStatus.Select(row => row.Status).ToArray());
            Assert.Equal(1, dashboard.DonationsByStatus.Single(row => row.Status == "Pending").Count);
            Assert.Equal(3m, dashboard.DonationsByStatus.Single(row => row.Status == "Pending").Amount);
            Assert.Equal(3, dashboard.DonationsByStatus.Single(row => row.Status == "Completed").Count);
            Assert.Equal(128m, dashboard.DonationsByStatus.Single(row => row.Status == "Completed").Amount);
            Assert.Equal(4m, dashboard.DonationsByStatus.Single(row => row.Status == "Failed").Amount);
            Assert.Equal(5m, dashboard.DonationsByStatus.Single(row => row.Status == "No payment").Amount);

            Assert.Equal(2, dashboard.TotalVisitors);
            Assert.Equal(await db.Context.Animals.CountAsync(), dashboard.TotalAnimals);
            Assert.Equal(5, dashboard.TotalReservations);
            Assert.Equal(3, dashboard.CompletedDonationCount);
            Assert.Equal(128m, dashboard.CompletedDonationAmount);
            Assert.Equal(22m, dashboard.TicketPaymentTotal);
            Assert.Equal(71m, dashboard.ReservationPaymentTotal);
            Assert.Equal(11m, dashboard.AdoptionPaymentTotal);
            Assert.Equal(128m, dashboard.DonationPaymentTotal);
            Assert.Equal(232m, dashboard.TotalRevenue);
            Assert.Equal(1, dashboard.ActiveAdoptions);
            Assert.Equal(animals[0].Category, Assert.Single(dashboard.AdoptionsByCategory).CategoryName);
            Assert.Equal(1, dashboard.AdoptionsByCategory[0].Count);

            Assert.Equal(12, dashboard.VisitorMonths.Count);
            Assert.Equal(1, dashboard.VisitorMonths.Single(point => point.Year == today.Year && point.Month == today.Month).Visitors);
            Assert.Equal(1, dashboard.VisitorMonths.Single(point => point.Year == previousMonth.Year && point.Month == previousMonth.Month).Visitors);

            Assert.Equal(12, dashboard.RevenueMonths.Count);
            var currentRevenue = dashboard.RevenueMonths.Single(point => point.Year == today.Year && point.Month == today.Month);
            Assert.Equal(15m, currentRevenue.TicketRevenue);
            Assert.Equal(52m, currentRevenue.ReservationRevenue);
            Assert.Equal(11m, currentRevenue.AdoptionRevenue);
            Assert.Equal(20m, currentRevenue.DonationRevenue);
            Assert.Equal(98m, currentRevenue.Total);
            var previousRevenue = dashboard.RevenueMonths.Single(point => point.Year == previousMonth.Year && point.Month == previousMonth.Month);
            Assert.Equal(7m, previousRevenue.TicketRevenue);
            Assert.Equal(10m, previousRevenue.ReservationRevenue);
            Assert.Equal(0m, previousRevenue.AdoptionRevenue);
            Assert.Equal(8m, previousRevenue.DonationRevenue);
            Assert.Equal(25m, previousRevenue.Total);
            Assert.DoesNotContain(
                dashboard.RevenueMonths,
                point => point.Year == outsideWindow.Year && point.Month == outsideWindow.Month);

            Assert.Equal(12, dashboard.RecentActivity.Count);
            for (var index = 1; index < dashboard.RecentActivity.Count; index++)
            {
                Assert.True(dashboard.RecentActivity[index - 1].OccurredAtUtc >= dashboard.RecentActivity[index].OccurredAtUtc);
            }

            var visitLabel = today.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("en-US"));
            var ticket = dashboard.RecentActivity.Single(item => item.Type == "Ticket" && item.Amount == 15m);
            Assert.Equal($"Dave Customer · Visit {visitLabel}", ticket.Label);
            Assert.Equal(DateTimeKind.Utc, ticket.OccurredAtUtc.Kind);
            Assert.Equal(today, ticket.OccurredAtUtc);
            var adoption = dashboard.RecentActivity.Single(item => item.Type == "Adoption" && item.Amount == 11m);
            Assert.Equal($"Dave Customer · {animals[0].Name}", adoption.Label);
            Assert.Contains(
                dashboard.RecentActivity,
                item => item.Type == "Reservation" && item.Label == "Dave Customer · Elephant Feeding Experience" && item.Amount == 40m);
            Assert.Contains(
                dashboard.RecentActivity,
                item => item.Type == "Donation" && item.Label == "Dave Customer · Sunbird Sanctuary" && item.Amount == 20m);
            Assert.DoesNotContain(
                typeof(DashboardRecentActivity).GetProperties(),
                property => property.Name.EndsWith("Id", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Dashboard_CountsVisitorsByVisitDate_OrCreationDate()
        {
            using var db = new SqliteDb();
            var now = DateTime.UtcNow;
            var today = new DateTime(now.Year, now.Month, now.Day, 12, 0, 0, DateTimeKind.Utc);
            var previousMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1).AddDays(10);
            var visitPayment = Payment(4m, PaymentStatus.Completed, today);
            var createdPayment = Payment(3m, PaymentStatus.Completed, today);
            var pendingPayment = Payment(9m, PaymentStatus.Pending, today);
            db.Context.Payments.AddRange(visitPayment, createdPayment, pendingPayment);
            await db.Context.SaveChangesAsync();

            var visitTicket = Ticket("TCK-VISIT", 4m, today, visitPayment.Id, TicketStatus.Active);
            visitTicket.VisitDate = DateOnly.FromDateTime(previousMonth);
            visitTicket.VisitorCount = 4;
            var createdTicket = Ticket("TCK-CREATED", 3m, today, createdPayment.Id, TicketStatus.Active);
            createdTicket.VisitDate = default;
            createdTicket.VisitorCount = 3;
            var unpaidTicket = Ticket("TCK-UNPAID", 9m, today, pendingPayment.Id, TicketStatus.PendingPayment);
            unpaidTicket.VisitorCount = 9;
            db.Context.Tickets.AddRange(visitTicket, createdTicket, unpaidTicket);
            await db.Context.SaveChangesAsync();

            var dashboard = await Dashboard(db.Context, admin: true, userId: 1).GetAsync();

            Assert.Equal(7, dashboard.TotalVisitors);
            Assert.Equal(3, dashboard.VisitorMonths.Single(point => point.Year == today.Year && point.Month == today.Month).Visitors);
            Assert.Equal(4, dashboard.VisitorMonths.Single(point => point.Year == previousMonth.Year && point.Month == previousMonth.Month).Visitors);
            var revenue = dashboard.RevenueMonths.Single(point => point.Year == today.Year && point.Month == today.Month);
            Assert.Equal(7m, revenue.TicketRevenue);
            Assert.Equal(7m, revenue.Total);
            var created = dashboard.RecentActivity.Single(item => item.Type == "Ticket" && item.Amount == 3m);
            Assert.Equal("Dave Customer · Ticket purchase", created.Label);
        }

        [Fact]
        public async Task Dashboard_RequiresAdministrator()
        {
            using var db = new SqliteDb();
            var error = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                Dashboard(db.Context, admin: false, userId: 4).GetAsync());
            Assert.Equal("Only administrators can view the dashboard.", error.Message);
        }

        [Fact]
        public async Task ReservationReport_SummarizesStatusRevenueAndFilters()
        {
            using var db = new SqliteDb();
            await SeedReportRows(db.Context);
            var service = Reports(db.Context, admin: true, userId: 1);

            var report = await service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30)
            });

            Assert.Equal("Alice Admin", report.GeneratedByName);
            Assert.Equal(3, report.TotalReservations);
            Assert.Equal(6, report.TotalGuests);
            Assert.Equal(30m, report.Revenue);
            Assert.Equal(1, report.StatusCounts.Single(row => row.Status == ReservationStatus.Pending).Count);
            Assert.Equal(1, report.StatusCounts.Single(row => row.Status == ReservationStatus.Confirmed).Count);
            Assert.Equal(0, report.StatusCounts.Single(row => row.Status == ReservationStatus.Completed).Count);
            Assert.Equal(1, report.StatusCounts.Single(row => row.Status == ReservationStatus.Cancelled).Count);
            Assert.Equal(new[] { "WN-CANC", "WN-PEND", "WN-PAID" }, report.Rows.Select(row => row.Code).ToArray());
            Assert.Equal("Dave Customer", report.Rows[0].CustomerName);
            Assert.Equal("Nocturnal Safari Walk", report.Rows[0].ActivityName);

            var activityTwo = await db.Context.Activities.AsNoTracking().SingleAsync(activity => activity.Name == "Nocturnal Safari Walk");
            var byActivity = await service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ActivityId = activityTwo.Id
            });
            Assert.Equal(activityTwo.Name, byActivity.ActivityName);
            Assert.Equal(new[] { "WN-CANC" }, byActivity.Rows.Select(row => row.Code).ToArray());
            Assert.Equal(0m, byActivity.Revenue);

            var confirmed = await service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                Status = ReservationStatus.Confirmed
            });
            Assert.Equal(new[] { "WN-PAID" }, confirmed.Rows.Select(row => row.Code).ToArray());
            Assert.Equal(30m, confirmed.Revenue);
        }

        [Fact]
        public async Task ReservationReport_EmptyRange_AndInvalidDates_AreRejected()
        {
            using var db = new SqliteDb();
            await SeedReportRows(db.Context);
            var service = Reports(db.Context, admin: true, userId: 1);

            var empty = await Assert.ThrowsAsync<ClientException>(() => service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 5, 2),
                DateTo = new DateOnly(2026, 5, 3)
            }));
            Assert.Equal(ReportService.EmptyReservationsMessage, empty.Message);

            var invalid = await Assert.ThrowsAsync<ValidationException>(() => service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 10),
                DateTo = new DateOnly(2026, 4, 1)
            }));
            Assert.Contains("From date cannot be after to date.", invalid.Errors.Select(error => error.ErrorMessage));

            var missing = await Assert.ThrowsAsync<NotFoundException>(() => service.BuildReservationReportAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ActivityId = 999
            }));
            Assert.Equal("Activity not found.", missing.Message);
        }

        [Fact]
        public async Task DonationReport_GroupsByPartner_AndFilters()
        {
            using var db = new SqliteDb();
            await SeedReportRows(db.Context);
            var service = Reports(db.Context, admin: true, userId: 1);

            var report = await service.BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30)
            });

            Assert.Equal("Alice Admin", report.GeneratedByName);
            Assert.Equal(2, report.TotalDonations);
            Assert.Equal(65m, report.GrandTotal);
            Assert.Equal(report.Partners.Sum(row => row.TotalAmount), report.GrandTotal);
            Assert.Equal(new[] { "Mountain Haven Rescue", "Sunbird Sanctuary" }, report.Partners.Select(row => row.PartnerName).ToArray());
            Assert.Equal(40m, report.Partners[0].TotalAmount);
            Assert.Equal(1, report.Partners[0].DonationCount);
            Assert.Equal("Nepal", report.Partners[0].Location);
            Assert.Equal(25m, report.Partners[1].TotalAmount);
            Assert.Equal(1, report.Partners[1].DonationCount);
            Assert.Equal(new[] { "Completed", "No payment", "Completed" }, report.Lines.Select(line => line.PaymentStatus).ToArray());
            Assert.Equal(new[] { 40m, 10m, 25m }, report.Lines.Select(line => line.Amount).ToArray());
            Assert.DoesNotContain(
                typeof(DonationReportLine).GetProperties(),
                property => property.Name.EndsWith("Id", StringComparison.Ordinal));

            var partner = await db.Context.ZooPartners.AsNoTracking().SingleAsync(row => row.Name == "Sunbird Sanctuary");
            var filtered = await service.BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ZooPartnerId = partner.Id
            });
            Assert.Equal("Sunbird Sanctuary", filtered.PartnerName);
            Assert.Equal(25m, filtered.GrandTotal);
            Assert.Equal(1, Assert.Single(filtered.Partners).DonationCount);
            Assert.Equal(2, filtered.Lines.Count);
            Assert.Contains(filtered.Lines, line => line.PaymentStatus == "No payment" && line.Amount == 10m);
            Assert.Contains(filtered.Lines, line => line.PaymentStatus == "Completed" && line.Amount == 25m);

            var pdf = await service.DonationsPdfAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30)
            });
            Assert.Equal("donations-by-partner-2026-04-01-to-2026-04-30.pdf", pdf.FileName);
            Assert.Equal("%PDF"u8.ToArray(), pdf.Content.Take(4).ToArray());
        }

        [Fact]
        public async Task DonationReport_EmptyRange_AndUnknownPartner_AreRejected()
        {
            using var db = new SqliteDb();
            db.Context.Donations.Add(new Donation
            {
                UserId = 4,
                ZooPartnerId = 1,
                Amount = 10m,
                CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            });
            await db.Context.SaveChangesAsync();
            var service = Reports(db.Context, admin: true, userId: 1);

            var empty = await Assert.ThrowsAsync<ClientException>(() => service.BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 1, 1),
                DateTo = new DateOnly(2026, 1, 2)
            }));
            Assert.Equal(ReportService.EmptyDonationsMessage, empty.Message);

            var missing = await Assert.ThrowsAsync<NotFoundException>(() => service.BuildDonationReportAsync(new DonationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30),
                ZooPartnerId = 999
            }));
            Assert.Equal("Zoo partner not found.", missing.Message);

            var forbidden = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                Reports(db.Context, admin: false, userId: 4).BuildDonationReportAsync(new DonationReportRequest
                {
                    DateFrom = new DateOnly(2026, 4, 1),
                    DateTo = new DateOnly(2026, 4, 30)
                }));
            Assert.Equal("Only administrators can generate reports.", forbidden.Message);
        }

        [Fact]
        public async Task ReservationPdf_StartsWithPdfHeader_AndNamesTheFile()
        {
            using var db = new SqliteDb();
            await SeedReportRows(db.Context);
            var pdf = await Reports(db.Context, admin: true, userId: 1).ReservationsPdfAsync(new ReservationReportRequest
            {
                DateFrom = new DateOnly(2026, 4, 1),
                DateTo = new DateOnly(2026, 4, 30)
            });

            Assert.Equal("reservations-2026-04-01-to-2026-04-30.pdf", pdf.FileName);
            Assert.True(pdf.Content.Length > 4);
            Assert.Equal("%PDF"u8.ToArray(), pdf.Content.Take(4).ToArray());
        }

        private static DashboardService Dashboard(ECommerceDbContext context, bool admin, int? userId) =>
            new(context, new ReportUserAccessor { Admin = admin, UserId = userId }, NullLogger<DashboardService>.Instance);

        private static ReportService Reports(ECommerceDbContext context, bool admin, int? userId) =>
            new(
                context,
                new ReportUserAccessor { Admin = admin, UserId = userId },
                new ReservationReportValidator(),
                new DonationReportValidator(),
                new QuestPdfReportRenderer(),
                NullLogger<ReportService>.Instance);

        private static async Task<int> ScheduleFor(ECommerceDbContext context, int activityId)
        {
            var existing = await context.Schedules.AsNoTracking()
                .Where(schedule => schedule.ActivityId == activityId)
                .Select(schedule => schedule.Id)
                .FirstAsync();
            return existing;
        }

        private static async Task SeedReportRows(ECommerceDbContext context)
        {
            var paid = Payment(30m, PaymentStatus.Completed, new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc));
            var refunded = Payment(15m, PaymentStatus.Refunded, new DateTime(2026, 4, 10, 15, 0, 0, DateTimeKind.Utc));
            var later = Payment(100m, PaymentStatus.Completed, new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));
            var donationPaid = Payment(25m, PaymentStatus.Completed, new DateTime(2026, 4, 5, 9, 0, 0, DateTimeKind.Utc));
            var donationPartnerTwo = Payment(40m, PaymentStatus.Completed, new DateTime(2026, 4, 7, 9, 0, 0, DateTimeKind.Utc));
            var donationLater = Payment(100m, PaymentStatus.Completed, new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));
            context.Payments.AddRange(paid, refunded, later, donationPaid, donationPartnerTwo, donationLater);
            await context.SaveChangesAsync();

            var activityOne = await context.Schedules.Where(schedule => schedule.ActivityId == 1).Select(schedule => schedule.Id).FirstAsync();
            var activityTwo = await context.Schedules.Where(schedule => schedule.ActivityId == 2).Select(schedule => schedule.Id).FirstAsync();
            context.Reservations.AddRange(
                Reservation("WN-PAID", activityOne, 2, 30m, ReservationStatus.Confirmed, new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc), paid.Id),
                Reservation("WN-PEND", activityOne, 1, 20m, ReservationStatus.Pending, new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc), null),
                Reservation("WN-CANC", activityTwo, 3, 15m, ReservationStatus.Cancelled, new DateTime(2026, 4, 10, 15, 0, 0, DateTimeKind.Utc), refunded.Id),
                Reservation("WN-LATER", activityOne, 4, 100m, ReservationStatus.Confirmed, new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc), later.Id));
            context.Donations.AddRange(
                Donation(1, 25m, new DateTime(2026, 4, 5, 9, 0, 0, DateTimeKind.Utc), donationPaid.Id),
                Donation(1, 10m, new DateTime(2026, 4, 6, 9, 0, 0, DateTimeKind.Utc), null),
                Donation(2, 40m, new DateTime(2026, 4, 7, 9, 0, 0, DateTimeKind.Utc), donationPartnerTwo.Id),
                Donation(1, 100m, new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc), donationLater.Id));
            await context.SaveChangesAsync();
        }

        private static Payment Payment(decimal amount, PaymentStatus status, DateTime createdAt) => new()
        {
            Method = PaymentMethod.Card,
            Amount = amount,
            Status = status,
            UserId = 4,
            CreatedAt = createdAt
        };

        private static Ticket Ticket(string code, decimal total, DateTime createdAt, int paymentId, TicketStatus status) => new()
        {
            UserId = 4,
            VisitDate = DateOnly.FromDateTime(createdAt),
            VisitorCount = 1,
            PricePerPerson = total,
            TotalPrice = total,
            Code = code,
            QrCodeData = code,
            Status = status,
            PaymentId = paymentId,
            CreatedAt = createdAt
        };

        private static Reservation Reservation(
            string code,
            int scheduleId,
            int guests,
            decimal total,
            ReservationStatus status,
            DateTime createdAt,
            int? paymentId) => new()
        {
            UserId = 4,
            ScheduleId = scheduleId,
            GuestCount = guests,
            TotalPrice = total,
            Status = status,
            Code = code,
            PaymentId = paymentId,
            CreatedAt = createdAt
        };

        private static Donation Donation(int partnerId, decimal amount, DateTime createdAt, int? paymentId) => new()
        {
            UserId = 4,
            ZooPartnerId = partnerId,
            Amount = amount,
            PaymentId = paymentId,
            CreatedAt = createdAt
        };

        private sealed class ReportUserAccessor : IAuthenticatedUserAccessor
        {
            public int? UserId { get; set; }
            public bool Admin { get; set; }
            public int? GetUserId() => UserId;
            public bool IsInRole(string role) => Admin && role == Roles.Admin;
        }
    }
}
