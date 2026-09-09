using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class AdoptionUpdateRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "MonthlyAmount must be greater than 0.")]
        public decimal MonthlyAmount { get; set; }

        public int? PaymentId { get; set; }
    }
}
