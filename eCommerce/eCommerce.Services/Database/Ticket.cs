using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Ticket
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public DateOnly VisitDate { get; set; }

        public int VisitorCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerPerson { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string QrCodeData { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Active;

        public DateTime? VerifiedAt { get; set; }

        public int? VerifiedByUserId { get; set; }

        [ForeignKey("VerifiedByUserId")]
        public User? VerifiedByUser { get; set; }

        public TicketVerificationMethod? VerificationMethod { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
