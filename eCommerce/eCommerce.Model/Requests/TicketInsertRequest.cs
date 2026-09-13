using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class TicketInsertRequest
    {
        [Required]
        public DateOnly VisitDate { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "VisitorCount must be greater than 0.")]
        public int VisitorCount { get; set; }
    }
}
