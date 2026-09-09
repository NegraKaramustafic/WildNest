using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Adoption
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int AnimalId { get; set; }

        [ForeignKey("AnimalId")]
        public Animal Animal { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyAmount { get; set; }

        public AdoptionStatus Status { get; set; } = AdoptionStatus.Active;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public int? PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }
    }
}
