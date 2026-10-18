namespace eCommerce.Model.Responses
{
    public class DashboardResponse
    {
        public int ReservationsToday { get; set; }
        public int ReservationsThisMonth { get; set; }
        public decimal TicketRevenueThisMonth { get; set; }
        public decimal DonationRevenueThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int ActiveSchedules { get; set; }
        public int NewUsersThisMonth { get; set; }
        public double? AverageRating { get; set; }
        public int TotalVisitors { get; set; }
        public int TotalAnimals { get; set; }
        public int TotalReservations { get; set; }
        public int CompletedDonationCount { get; set; }
        public decimal CompletedDonationAmount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TicketPaymentTotal { get; set; }
        public decimal ReservationPaymentTotal { get; set; }
        public decimal AdoptionPaymentTotal { get; set; }
        public decimal DonationPaymentTotal { get; set; }
        public int ActiveAdoptions { get; set; }
        public List<DashboardMonthPoint> Months { get; set; } = new();
        public List<DashboardActivityPoint> TopActivities { get; set; } = new();
        public List<DashboardDonationStatusPoint> DonationsByStatus { get; set; } = new();
        public List<DashboardVisitorMonthPoint> VisitorMonths { get; set; } = new();
        public List<DashboardRevenueMonthPoint> RevenueMonths { get; set; } = new();
        public List<DashboardCategoryPoint> AdoptionsByCategory { get; set; } = new();
        public List<DashboardRecentActivity> RecentActivity { get; set; } = new();
    }

    public class DashboardMonthPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ReservationCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardActivityPoint
    {
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class DashboardDonationStatusPoint
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class DashboardVisitorMonthPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Visitors { get; set; }
    }

    public class DashboardRevenueMonthPoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TicketRevenue { get; set; }
        public decimal ReservationRevenue { get; set; }
        public decimal AdoptionRevenue { get; set; }
        public decimal DonationRevenue { get; set; }
        public decimal Total { get; set; }
    }

    public class DashboardCategoryPoint
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardRecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    }
}
