using System.ComponentModel.DataAnnotations;

namespace eCommerce.Model.Requests
{
    public class AnimalCategoryUpdateRequest
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
    }
}
