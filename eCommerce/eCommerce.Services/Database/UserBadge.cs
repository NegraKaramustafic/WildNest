using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    public class UserBadge
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        public int BadgeId { get; set; }

        [ForeignKey("BadgeId")]
        public Badge Badge { get; set; } = null!;

        public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
    }
}
