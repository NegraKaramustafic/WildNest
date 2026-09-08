using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class ReservationUpdateRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "GuestCount must be greater than 0.")]
        public int GuestCount { get; set; }
    }
}
