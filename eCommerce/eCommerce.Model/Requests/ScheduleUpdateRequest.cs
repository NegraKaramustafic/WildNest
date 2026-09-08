using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class ScheduleUpdateRequest
    {
        [Required]
        public int ActivityId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public int? CapacityLeft { get; set; }

        public ScheduleStatus Status { get; set; } = ScheduleStatus.Active;
    }
}
