using System.Globalization;
using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eCommerce.Services
{
    public class DashboardService : IDashboardService
    {
        private const int TopActivityCount = 5;
        private const int RecentActivityCount = 12;
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");
        private static readonly DateOnly MissingVisitDate = DateOnly.MinValue;

        private static readonly string[] DonationStatusOrder =
        [
            "Pending",
            "Completed",
            "Failed",
            "Refunded",
            "No payment"
        ];

        private readonly ECommerceDbContext _dbContext;
        private readonly IAuthenticatedUserAccessor _userAccessor;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            ECommerceDbContext dbContext,
            IAuthenticatedUserAccessor userAccessor,
            ILogger<DashboardService> logger)
        {
            _dbContext = dbContext;
            _userAccessor = userAccessor;
            _logger = logger;
        }

        public async Task<DashboardResponse> GetAsync()
        {
            if (!_userAccessor.IsInRole(Roles.Admin))
            {
                _logger.LogWarning("A non-administrator requested the dashboard.");
                throw new UnauthorizedAccessException("Only administrators can view the dashboard.");
            }

            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var tomorrow = todayStart.AddDays(1);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonth = monthStart.AddMonths(1);
            var trendStart = monthStart.AddMonths(-11);

            var reservationsToday = await _dbContext.Reservations.AsNoTracking()
                .CountAsync(reservation => reservation.CreatedAt >= todayStart && reservation.CreatedAt < tomorrow);

            var reservationsThisMonth = await _dbContext.Reservations.AsNoTracking()
                .CountAsync(reservation => reservation.CreatedAt >= monthStart && reservation.CreatedAt < nextMonth);

            var ticketRevenueThisMonth = await CompletedTicketRevenue(_dbContext, monthStart, nextMonth);
            var donationRevenueThisMonth = await CompletedDonationRevenue(_dbContext, monthStart, nextMonth);

            var activeSchedules = await _dbContext.Schedules.AsNoTracking()
                .CountAsync(schedule => schedule.Status == ScheduleStatus.Active);

            var newUsersThisMonth = await _dbContext.Users.AsNoTracking()
                .CountAsync(user => user.CreatedAt >= monthStart && user.CreatedAt < nextMonth);

            var averageRating = await _dbContext.Reviews.AsNoTracking()
                .AverageAsync(review => (double?)review.Rating);

            var reservationMonths = await _dbContext.Reservations.AsNoTracking()
                .Where(reservation => reservation.CreatedAt >= trendStart && reservation.CreatedAt < nextMonth)
                .GroupBy(reservation => new { reservation.CreatedAt.Year, reservation.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Count = group.Count()
                })
                .ToListAsync();

            var ticketMonths = await _dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.CreatedAt >= trendStart && ticket.CreatedAt < nextMonth)
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                .GroupBy(ticket => new { ticket.CreatedAt.Year, ticket.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Amount = group.Sum(ticket => ticket.TotalPrice)
                })
                .ToListAsync();

            var donationMonths = await _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.CreatedAt >= trendStart && donation.CreatedAt < nextMonth)
                .Where(donation => donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed)
                .GroupBy(donation => new { donation.CreatedAt.Year, donation.CreatedAt.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Amount = group.Sum(donation => donation.Amount)
                })
                .ToListAsync();

            var reservationLookup = reservationMonths.ToDictionary(row => (row.Year, row.Month), row => row.Count);
            var ticketLookup = ticketMonths.ToDictionary(row => (row.Year, row.Month), row => row.Amount);
            var donationLookup = donationMonths.ToDictionary(row => (row.Year, row.Month), row => row.Amount);

            var months = new List<DashboardMonthPoint>();
            for (var cursor = trendStart; cursor < nextMonth; cursor = cursor.AddMonths(1))
            {
                var key = (cursor.Year, cursor.Month);
                reservationLookup.TryGetValue(key, out var reservationCount);
                ticketLookup.TryGetValue(key, out var ticketRevenue);
                donationLookup.TryGetValue(key, out var donationRevenue);
                months.Add(new DashboardMonthPoint
                {
                    Year = cursor.Year,
                    Month = cursor.Month,
                    ReservationCount = reservationCount,
                    Revenue = ticketRevenue + donationRevenue
                });
            }

            var topRows = await _dbContext.Reservations.AsNoTracking()
                .Where(reservation => reservation.CreatedAt >= trendStart && reservation.CreatedAt < nextMonth)
                .Where(reservation => reservation.Status != ReservationStatus.Cancelled)
                .GroupBy(reservation => new { reservation.Schedule.ActivityId, reservation.Schedule.Activity.Name })
                .Select(group => new
                {
                    group.Key.ActivityId,
                    group.Key.Name,
                    Count = group.Count(),
                    Revenue = group.Sum(reservation =>
                        reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Completed
                            ? reservation.TotalPrice
                            : 0m)
                })
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.Name)
                .ThenBy(row => row.ActivityId)
                .Take(TopActivityCount)
                .ToListAsync();

            var paidDonations = await (
                from donation in _dbContext.Donations.AsNoTracking()
                join payment in _dbContext.Payments.AsNoTracking() on donation.PaymentId equals payment.Id
                group donation by payment.Status
                into grouped
                select new
                {
                    Status = grouped.Key,
                    Count = grouped.Count(),
                    Amount = grouped.Sum(donation => donation.Amount)
                }).ToListAsync();

            var unpaid = await _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.PaymentId == null)
                .GroupBy(_ => 1)
                .Select(group => new
                {
                    Count = group.Count(),
                    Amount = group.Sum(donation => donation.Amount)
                })
                .FirstOrDefaultAsync();

            var donationsByStatus = paidDonations
                .Select(row => new DashboardDonationStatusPoint
                {
                    Status = PaymentStatusLabel(row.Status),
                    Count = row.Count,
                    Amount = row.Amount
                })
                .Where(row => row.Count > 0)
                .ToList();

            if (unpaid is { Count: > 0 })
            {
                donationsByStatus.Add(new DashboardDonationStatusPoint
                {
                    Status = "No payment",
                    Count = unpaid.Count,
                    Amount = unpaid.Amount
                });
            }

            donationsByStatus = donationsByStatus
                .OrderBy(row => Array.IndexOf(DonationStatusOrder, row.Status))
                .ThenBy(row => row.Status)
                .ToList();

            var overview = await LoadOverviewAsync();
            var visitorMonths = await LoadVisitorMonthsAsync(trendStart, nextMonth);
            var revenueMonths = await LoadRevenueMonthsAsync(trendStart, nextMonth);

            return new DashboardResponse
            {
                ReservationsToday = reservationsToday,
                ReservationsThisMonth = reservationsThisMonth,
                TicketRevenueThisMonth = ticketRevenueThisMonth,
                DonationRevenueThisMonth = donationRevenueThisMonth,
                RevenueThisMonth = ticketRevenueThisMonth + donationRevenueThisMonth,
                ActiveSchedules = activeSchedules,
                NewUsersThisMonth = newUsersThisMonth,
                AverageRating = averageRating == null
                    ? null
                    : Math.Round(averageRating.Value, 1, MidpointRounding.AwayFromZero),
                TotalVisitors = overview.Visitors,
                TotalAnimals = overview.Animals,
                TotalReservations = overview.Reservations,
                CompletedDonationCount = overview.CompletedDonationCount,
                CompletedDonationAmount = overview.CompletedDonationAmount,
                TotalRevenue = overview.TicketPayments + overview.ReservationPayments + overview.AdoptionPayments + overview.DonationPayments,
                TicketPaymentTotal = overview.TicketPayments,
                ReservationPaymentTotal = overview.ReservationPayments,
                AdoptionPaymentTotal = overview.AdoptionPayments,
                DonationPaymentTotal = overview.DonationPayments,
                ActiveAdoptions = overview.ActiveAdoptions,
                Months = months,
                TopActivities = topRows.Select(row => new DashboardActivityPoint
                {
                    ActivityId = row.ActivityId,
                    ActivityName = row.Name,
                    ReservationCount = row.Count,
                    Revenue = row.Revenue
                }).ToList(),
                DonationsByStatus = donationsByStatus,
                VisitorMonths = visitorMonths,
                RevenueMonths = revenueMonths,
                AdoptionsByCategory = await LoadAdoptionsByCategoryAsync(),
                RecentActivity = await LoadRecentActivityAsync()
            };
        }

        private static async Task<decimal> CompletedTicketRevenue(ECommerceDbContext dbContext, DateTime from, DateTime toExclusive)
        {
            return await dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.CreatedAt >= from && ticket.CreatedAt < toExclusive)
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                .SumAsync(ticket => (decimal?)ticket.TotalPrice) ?? 0m;
        }

        private static async Task<decimal> CompletedDonationRevenue(ECommerceDbContext dbContext, DateTime from, DateTime toExclusive)
        {
            return await dbContext.Donations.AsNoTracking()
                .Where(donation => donation.CreatedAt >= from && donation.CreatedAt < toExclusive)
                .Where(donation => donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed)
                .SumAsync(donation => (decimal?)donation.Amount) ?? 0m;
        }

        private async Task<OverviewTotals> LoadOverviewAsync()
        {
            var completedDonations = _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed);

            return new OverviewTotals(
                Visitors: await _dbContext.Tickets.AsNoTracking()
                    .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                    .SumAsync(ticket => (int?)ticket.VisitorCount) ?? 0,
                Animals: await _dbContext.Animals.CountAsync(),
                Reservations: await _dbContext.Reservations.CountAsync(),
                CompletedDonationCount: await completedDonations.CountAsync(),
                CompletedDonationAmount: await completedDonations.SumAsync(donation => (decimal?)donation.Amount) ?? 0m,
                TicketPayments: await SumTicketPayments(),
                ReservationPayments: await SumReservationPayments(),
                AdoptionPayments: await SumAdoptionPayments(),
                DonationPayments: await SumDonationPayments(),
                ActiveAdoptions: await _dbContext.Adoptions.CountAsync(adoption => adoption.Status == AdoptionStatus.Active));
        }

        private async Task<decimal> SumTicketPayments() =>
            await _dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                .SumAsync(ticket => (decimal?)ticket.Payment!.Amount) ?? 0m;

        private async Task<decimal> SumReservationPayments() =>
            await _dbContext.Reservations.AsNoTracking()
                .Where(reservation => reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Completed)
                .SumAsync(reservation => (decimal?)reservation.Payment!.Amount) ?? 0m;

        private async Task<decimal> SumAdoptionPayments() =>
            await _dbContext.Adoptions.AsNoTracking()
                .Where(adoption => adoption.Payment != null && adoption.Payment.Status == PaymentStatus.Completed)
                .SumAsync(adoption => (decimal?)adoption.Payment!.Amount) ?? 0m;

        private async Task<decimal> SumDonationPayments() =>
            await _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed)
                .SumAsync(donation => (decimal?)donation.Payment!.Amount) ?? 0m;

        private async Task<List<DashboardVisitorMonthPoint>> LoadVisitorMonthsAsync(DateTime trendStart, DateTime nextMonth)
        {
            var trendDate = DateOnly.FromDateTime(trendStart);
            var nextDate = DateOnly.FromDateTime(nextMonth);
            var completed = _dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed);

            var byVisit = await completed
                .Where(ticket => ticket.VisitDate != MissingVisitDate)
                .Where(ticket => ticket.VisitDate >= trendDate && ticket.VisitDate < nextDate)
                .GroupBy(ticket => new { ticket.VisitDate.Year, ticket.VisitDate.Month })
                .Select(group => new { group.Key.Year, group.Key.Month, Visitors = group.Sum(ticket => ticket.VisitorCount) })
                .ToListAsync();

            var byCreated = await completed
                .Where(ticket => ticket.VisitDate == MissingVisitDate)
                .Where(ticket => ticket.CreatedAt >= trendStart && ticket.CreatedAt < nextMonth)
                .GroupBy(ticket => new { ticket.CreatedAt.Year, ticket.CreatedAt.Month })
                .Select(group => new { group.Key.Year, group.Key.Month, Visitors = group.Sum(ticket => ticket.VisitorCount) })
                .ToListAsync();

            return Enumerable.Range(0, 12).Select(offset =>
            {
                var month = trendStart.AddMonths(offset);
                var visitors = byVisit.Where(row => row.Year == month.Year && row.Month == month.Month).Sum(row => row.Visitors)
                    + byCreated.Where(row => row.Year == month.Year && row.Month == month.Month).Sum(row => row.Visitors);
                return new DashboardVisitorMonthPoint
                {
                    Year = month.Year,
                    Month = month.Month,
                    Visitors = visitors
                };
            }).ToList();
        }

        private async Task<List<DashboardRevenueMonthPoint>> LoadRevenueMonthsAsync(DateTime trendStart, DateTime nextMonth)
        {
            var tickets = await _dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                .Where(ticket => ticket.Payment!.CreatedAt >= trendStart && ticket.Payment.CreatedAt < nextMonth)
                .GroupBy(ticket => new { ticket.Payment!.CreatedAt.Year, ticket.Payment.CreatedAt.Month })
                .Select(group => new MonthAmount { Year = group.Key.Year, Month = group.Key.Month, Amount = group.Sum(ticket => ticket.Payment!.Amount) })
                .ToListAsync();
            var reservations = await _dbContext.Reservations.AsNoTracking()
                .Where(reservation => reservation.Payment != null && reservation.Payment.Status == PaymentStatus.Completed)
                .Where(reservation => reservation.Payment!.CreatedAt >= trendStart && reservation.Payment.CreatedAt < nextMonth)
                .GroupBy(reservation => new { reservation.Payment!.CreatedAt.Year, reservation.Payment.CreatedAt.Month })
                .Select(group => new MonthAmount { Year = group.Key.Year, Month = group.Key.Month, Amount = group.Sum(reservation => reservation.Payment!.Amount) })
                .ToListAsync();
            var adoptions = await _dbContext.Adoptions.AsNoTracking()
                .Where(adoption => adoption.Payment != null && adoption.Payment.Status == PaymentStatus.Completed)
                .Where(adoption => adoption.Payment!.CreatedAt >= trendStart && adoption.Payment.CreatedAt < nextMonth)
                .GroupBy(adoption => new { adoption.Payment!.CreatedAt.Year, adoption.Payment.CreatedAt.Month })
                .Select(group => new MonthAmount { Year = group.Key.Year, Month = group.Key.Month, Amount = group.Sum(adoption => adoption.Payment!.Amount) })
                .ToListAsync();
            var donations = await _dbContext.Donations.AsNoTracking()
                .Where(donation => donation.Payment != null && donation.Payment.Status == PaymentStatus.Completed)
                .Where(donation => donation.Payment!.CreatedAt >= trendStart && donation.Payment.CreatedAt < nextMonth)
                .GroupBy(donation => new { donation.Payment!.CreatedAt.Year, donation.Payment.CreatedAt.Month })
                .Select(group => new MonthAmount { Year = group.Key.Year, Month = group.Key.Month, Amount = group.Sum(donation => donation.Payment!.Amount) })
                .ToListAsync();

            return Enumerable.Range(0, 12).Select(offset =>
            {
                var month = trendStart.AddMonths(offset);
                var ticketRevenue = AmountFor(tickets, month);
                var reservationRevenue = AmountFor(reservations, month);
                var adoptionRevenue = AmountFor(adoptions, month);
                var donationRevenue = AmountFor(donations, month);
                return new DashboardRevenueMonthPoint
                {
                    Year = month.Year,
                    Month = month.Month,
                    TicketRevenue = ticketRevenue,
                    ReservationRevenue = reservationRevenue,
                    AdoptionRevenue = adoptionRevenue,
                    DonationRevenue = donationRevenue,
                    Total = ticketRevenue + reservationRevenue + adoptionRevenue + donationRevenue
                };
            }).ToList();
        }

        private static decimal AmountFor(IReadOnlyList<MonthAmount> rows, DateTime month) =>
            rows.Where(row => row.Year == month.Year && row.Month == month.Month).Sum(row => row.Amount);

        private async Task<List<DashboardCategoryPoint>> LoadAdoptionsByCategoryAsync()
        {
            var rows = await _dbContext.Adoptions.AsNoTracking()
                .Where(adoption => adoption.Status == AdoptionStatus.Active)
                .GroupBy(adoption => adoption.Animal.Category.Name)
                .Select(group => new { CategoryName = group.Key, Count = group.Count() })
                .ToListAsync();

            return rows
                .OrderByDescending(row => row.Count)
                .ThenBy(row => row.CategoryName)
                .Select(row => new DashboardCategoryPoint
                {
                    CategoryName = row.CategoryName,
                    Count = row.Count
                })
                .ToList();
        }

        private async Task<List<DashboardRecentActivity>> LoadRecentActivityAsync()
        {
            var reservations = await _dbContext.Reservations.AsNoTracking()
                .OrderByDescending(reservation => reservation.CreatedAt)
                .ThenByDescending(reservation => reservation.Id)
                .Take(RecentActivityCount)
                .Select(reservation => new DashboardRecentActivity
                {
                    Type = "Reservation",
                    Label = reservation.User.FirstName + " " + reservation.User.LastName + " · " + reservation.Schedule.Activity.Name,
                    Amount = reservation.TotalPrice,
                    OccurredAtUtc = reservation.CreatedAt
                })
                .ToListAsync();

            var donations = await _dbContext.Donations.AsNoTracking()
                .OrderByDescending(donation => donation.CreatedAt)
                .ThenByDescending(donation => donation.Id)
                .Take(RecentActivityCount)
                .Select(donation => new DashboardRecentActivity
                {
                    Type = "Donation",
                    Label = donation.User.FirstName + " " + donation.User.LastName + " · " + donation.ZooPartner.Name,
                    Amount = donation.Amount,
                    OccurredAtUtc = donation.CreatedAt
                })
                .ToListAsync();

            var adoptions = await _dbContext.Adoptions.AsNoTracking()
                .OrderByDescending(adoption => adoption.StartDate)
                .ThenByDescending(adoption => adoption.Id)
                .Take(RecentActivityCount)
                .Select(adoption => new DashboardRecentActivity
                {
                    Type = "Adoption",
                    Label = adoption.User.FirstName + " " + adoption.User.LastName + " · " + adoption.Animal.Name,
                    Amount = adoption.MonthlyAmount,
                    OccurredAtUtc = adoption.StartDate
                })
                .ToListAsync();

            var tickets = await _dbContext.Tickets.AsNoTracking()
                .Where(ticket => ticket.Payment != null && ticket.Payment.Status == PaymentStatus.Completed)
                .OrderByDescending(ticket => ticket.CreatedAt)
                .ThenByDescending(ticket => ticket.Id)
                .Take(RecentActivityCount)
                .Select(ticket => new
                {
                    Name = ticket.User.FirstName + " " + ticket.User.LastName,
                    ticket.VisitDate,
                    ticket.TotalPrice,
                    ticket.CreatedAt
                })
                .ToListAsync();

            var ticketEvents = tickets.Select(ticket => new DashboardRecentActivity
            {
                Type = "Ticket",
                Label = ticket.VisitDate == MissingVisitDate
                    ? ticket.Name + " · Ticket purchase"
                    : ticket.Name + " · Visit " + ticket.VisitDate.ToString("dd MMM yyyy", English),
                Amount = ticket.TotalPrice,
                OccurredAtUtc = ticket.CreatedAt
            });

            return reservations
                .Concat(donations)
                .Concat(adoptions)
                .Concat(ticketEvents)
                .Select(item =>
                {
                    item.OccurredAtUtc = DateTime.SpecifyKind(item.OccurredAtUtc, DateTimeKind.Utc);
                    return item;
                })
                .OrderByDescending(item => item.OccurredAtUtc)
                .ThenBy(item => item.Type)
                .ThenBy(item => item.Label)
                .Take(RecentActivityCount)
                .ToList();
        }

        private static string PaymentStatusLabel(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Pending",
            PaymentStatus.Completed => "Completed",
            PaymentStatus.Failed => "Failed",
            PaymentStatus.Refunded => "Refunded",
            _ => status.ToString()
        };

        private sealed record OverviewTotals(
            int Visitors,
            int Animals,
            int Reservations,
            int CompletedDonationCount,
            decimal CompletedDonationAmount,
            decimal TicketPayments,
            decimal ReservationPayments,
            decimal AdoptionPayments,
            decimal DonationPayments,
            int ActiveAdoptions);

        private sealed class MonthAmount
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
