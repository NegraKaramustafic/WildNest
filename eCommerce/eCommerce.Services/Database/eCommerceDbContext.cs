using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<AnimalCategory> AnimalCategories { get; set; }
        public DbSet<Animal> Animals { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<StripeProcessedEvent> StripeProcessedEvents { get; set; }
        public DbSet<ZooPartner> ZooPartners { get; set; }
        public DbSet<Adoption> Adoptions { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Badge> Badges { get; set; }
        public DbSet<UserBadge> UserBadges { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            CreateConfiguration(modelBuilder);

            CreateSeed(modelBuilder);
            
        }

       
    }
}
