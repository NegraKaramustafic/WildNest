using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class ReservationResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public int ScheduleId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public DateOnly ScheduleDate { get; set; }
        public TimeOnly ScheduleStartTime { get; set; }
        public int GuestCount { get; set; }
        public decimal TotalPrice { get; set; }
        public ReservationStatus Status { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? ActivityImageUrl { get; set; }
        public int? PaymentId { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ConfirmedByUserId { get; set; }
        public string? ConfirmedByFullName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int? CancelledByUserId { get; set; }
        public string? CancelledByFullName { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancellationReason { get; set; }
        public List<string> AllowedActions { get; set; } = new List<string>();
    }
}
