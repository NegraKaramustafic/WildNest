using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class TicketUpdateRequest
    {
        [Required]
        public DateOnly VisitDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VisitorCount must be greater than 0.")]
        public int VisitorCount { get; set; }

        [Required]
        public decimal PricePerPerson { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Active;
    }
}
