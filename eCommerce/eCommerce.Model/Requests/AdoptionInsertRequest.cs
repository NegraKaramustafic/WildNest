using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class AdoptionInsertRequest
    {
        [Required]
        public int AnimalId { get; set; }

        public int? PaymentId { get; set; }
    }
}
