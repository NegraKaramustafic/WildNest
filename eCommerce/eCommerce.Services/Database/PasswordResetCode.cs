using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eCommerce.Services.Database
{
    public class PasswordResetCode
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string CodeHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string CodeSalt { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? InvalidatedAt { get; set; }

        public int FailedAttempts { get; set; }
    }
}
