using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class TicketVerifyRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        public TicketVerificationMethod Method { get; set; }
    }
}
