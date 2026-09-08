using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ReservationInsertRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "GuestCount must be greater than 0.")]
        public int GuestCount { get; set; }
    }
}
