using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class ReservationReportDocument
    {
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public string? ActivityName { get; set; }
        public ReservationStatus? Status { get; set; }
        public string GeneratedByName { get; set; } = string.Empty;
        public DateTime GeneratedAtUtc { get; set; }
        public int TotalReservations { get; set; }
        public int TotalGuests { get; set; }
        public decimal Revenue { get; set; }
        public List<ReservationStatusCount> StatusCounts { get; set; } = new();
        public List<ReservationReportRow> Rows { get; set; } = new();
    }

    public class ReservationStatusCount
    {
        public ReservationStatus Status { get; set; }
        public int Count { get; set; }
    }

    public class ReservationReportRow
    {
        public string Code { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public DateOnly ScheduleDate { get; set; }
        public TimeOnly ScheduleStartTime { get; set; }
        public int GuestCount { get; set; }
        public ReservationStatus Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
