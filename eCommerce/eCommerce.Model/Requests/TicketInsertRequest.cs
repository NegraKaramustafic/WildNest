using System.ComponentModel.DataAnnotations;
using eCommerce.Model;

namespace eCommerce.Model.Requests
{
    public class TicketInsertRequest
    {
        [Required]
        public DateOnly VisitDate { get; set; }

        [Required]
        [Range(TicketLimits.MinimumVisitors, TicketLimits.MaximumVisitors, ErrorMessage = TicketLimits.VisitorCountMessage)]
        public int VisitorCount { get; set; }
    }
}
