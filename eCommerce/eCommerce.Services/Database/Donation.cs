using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    public class Donation
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int ZooPartnerId { get; set; }

        [ForeignKey("ZooPartnerId")]
        public ZooPartner ZooPartner { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public int? PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
