using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class AnimalCategoryInsertRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
