using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eCommerce.Model.Enums;

namespace eCommerce.Services.Database
{
    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int ScheduleId { get; set; }

        [ForeignKey("ScheduleId")]
        public Schedule Schedule { get; set; } = null!;

        public int GuestCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        [Required]
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;

        public int? PaymentId { get; set; }

        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
