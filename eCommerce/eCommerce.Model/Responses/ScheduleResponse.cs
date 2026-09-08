using eCommerce.Model.Enums;

namespace eCommerce.Model.Responses
{
    public class ScheduleResponse
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public string ActivityName { get; set; } = string.Empty;
        public decimal ActivityPrice { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int CapacityLeft { get; set; }
        public ScheduleStatus Status { get; set; }
    }
}
