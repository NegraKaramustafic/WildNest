using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class AdoptionInsertRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int AnimalId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "MonthlyAmount must be greater than 0.")]
        public decimal? MonthlyAmount { get; set; }

        public int? PaymentId { get; set; }
    }
}
