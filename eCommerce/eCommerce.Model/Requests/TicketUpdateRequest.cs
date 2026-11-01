using System.ComponentModel.DataAnnotations;
using eCommerce.Model;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class TicketUpdateRequest
    {
        [Required]
        public DateOnly VisitDate { get; set; }

        [Required]
        [Range(TicketLimits.MinimumVisitors, TicketLimits.MaximumVisitors, ErrorMessage = TicketLimits.VisitorCountMessage)]
        public int VisitorCount { get; set; }

        [Required]
        public decimal PricePerPerson { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Active;
    }
}
