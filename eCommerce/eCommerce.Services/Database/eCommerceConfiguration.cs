using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {

        private void CreateConfiguration(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(ur => ur.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(ur => ur.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Animal>()
                .HasOne(a => a.Category)
                .WithMany(c => c.Animals)
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActivityType>()
                .HasIndex(t => t.Name)
                .IsUnique();

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.Animal)
                .WithMany()
                .HasForeignKey(a => a.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Activity>()
                .HasOne(a => a.ActivityType)
                .WithMany(t => t.Activities)
                .HasForeignKey(a => a.ActivityTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Activity)
                .WithMany(a => a.Schedules)
                .HasForeignKey(s => s.ActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.Code)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.VerifiedByUser)
                .WithMany()
                .HasForeignKey(t => t.VerifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.Code)
                .IsUnique();

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Schedule)
                .WithMany()
                .HasForeignKey(r => r.ScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.StripePaymentIntentId);

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.StripeCheckoutSessionId);

            modelBuilder.Entity<StripeProcessedEvent>()
                .HasIndex(e => e.EventId)
                .IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.ConfirmedByUser)
                .WithMany()
                .HasForeignKey(r => r.ConfirmedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.CancelledByUser)
                .WithMany()
                .HasForeignKey(r => r.CancelledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Adoption>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Adoption>()
                .HasOne(a => a.Animal)
                .WithMany()
                .HasForeignKey(a => a.AnimalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Adoption>()
                .HasOne(a => a.Payment)
                .WithMany()
                .HasForeignKey(a => a.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Adoption>()
                .HasOne(a => a.RejectedByUser)
                .WithMany()
                .HasForeignKey(a => a.RejectedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.ZooPartner)
                .WithMany()
                .HasForeignKey(d => d.ZooPartnerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Payment)
                .WithMany()
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasIndex(r => new { r.UserId, r.ActivityId })
                .IsUnique();

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Activity)
                .WithMany()
                .HasForeignKey(r => r.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBadge>()
                .HasIndex(ub => new { ub.UserId, ub.BadgeId })
                .IsUnique();

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.User)
                .WithMany()
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserBadge>()
                .HasOne(ub => ub.Badge)
                .WithMany()
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.MessageId)
                .IsUnique();

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<PasswordResetCode>()
                .HasOne(code => code.User)
                .WithMany()
                .HasForeignKey(code => code.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PasswordResetCode>()
                .HasIndex(code => new { code.UserId, code.CreatedAt });
        }
    }
}
