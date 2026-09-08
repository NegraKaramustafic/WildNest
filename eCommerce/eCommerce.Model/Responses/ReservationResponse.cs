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
        public DateTime CreatedAt { get; set; }
        public List<string> AllowedActions { get; set; } = new List<string>();
    }
}
