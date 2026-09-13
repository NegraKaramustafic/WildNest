using System.ComponentModel.DataAnnotations;
using eCommerce.Model.Enums;

namespace eCommerce.Model.Requests
{
    public class PaymentCheckoutRequest
    {
        [Required]
        public PaymentRelatedEntityType RelatedEntityType { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "RelatedEntityId must be greater than 0.")]
        public int RelatedEntityId { get; set; }
    }
}
