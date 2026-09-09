using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class DonationInsertRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ZooPartnerId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public int? PaymentId { get; set; }
    }
}
