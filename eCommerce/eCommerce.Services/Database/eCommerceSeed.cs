using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateSeed(ModelBuilder modelBuilder)
        {
            SeedRoles(modelBuilder);
            SeedUsers(modelBuilder);
            SeedUserRoles(modelBuilder);
            SeedAnimalCategories(modelBuilder);
            SeedAnimals(modelBuilder);
            SeedActivityTypes(modelBuilder);
            SeedActivities(modelBuilder);
            SeedSchedules(modelBuilder);
            SeedZooPartners(modelBuilder);
            SeedReviews(modelBuilder);
            SeedBadges(modelBuilder);
            SeedNews(modelBuilder);
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            // Seed Roles - deterministic Ids: 1 = Admin, 2 = Customer
            modelBuilder.Entity<Role>().HasData(
                new
                {
                    Id = 1,
                    Name = global::eCommerce.Model.Roles.Admin,
                    Description = "Administrator role with full permissions",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 2,
                    Name = global::eCommerce.Model.Roles.Customer,
                    Description = "Default customer role",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        private void SeedUsers(ModelBuilder modelBuilder)
        {
            // Seed Users - 3 admins (Ids 1-3) and 2 customers (Ids 4-5)
            modelBuilder.Entity<User>().HasData(
                new
                {
                    Id = 1,
                    FirstName = "Alice",
                    LastName = "Admin",
                    Email = "admin1@gmail.com",
                    Username = "admin1",
                    PasswordHash = "5kRBQg4Ufcx4hAknG7P9zhfLPvY=", // Test123
                    PasswordSalt = "FmvmUwPsJyRRffhNRQvbrA==",
                    IsActive = true,
                    SecurityStamp = "seedstamp000000000000000000000001",
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = (DateTime?)null,
                    PhoneNumber = (string?)null
                },
                new
                {
                    Id = 2,
                    FirstName = "Bob",
                    LastName = "Admin",
                    Email = "admin2@gmail.com",
                    Username = "admin2",
                    PasswordHash = "GBoyh1WP+OMgGjqRj6vK6L1+oGc=", // Test123
                    PasswordSalt = "0AXpKx6xRp9xM42jCf/PiA==",
                    IsActive = true,
                    SecurityStamp = "seedstamp000000000000000000000002",
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = (DateTime?)null,
                    PhoneNumber = (string?)null
                },
                new
                {
                    Id = 3,
                    FirstName = "Carol",
                    LastName = "Admin",
                    Email = "admin3@gmail.com",
                    Username = "admin3",
                    PasswordHash = "x6JHKCTQywdAzTcZxGWFvrKPORM=", // Test123
                    PasswordSalt = "IwhTfKQNgyqWfOlTqCDXrg==",
                    IsActive = true,
                    SecurityStamp = "seedstamp000000000000000000000003",
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = (DateTime?)null,
                    PhoneNumber = (string?)null
                },
                new
                {
                    Id = 4,
                    FirstName = "Dave",
                    LastName = "Customer",
                    Email = "customer1@gmail.com",
                    Username = "customer1",
                    PasswordHash = "E0fA2/f9GZvIRRt/cgqQemG/Cog=", // Test123
                    PasswordSalt = "TiJxWTJcd7sBSiWNbhK9Vw==",
                    IsActive = true,
                    SecurityStamp = "seedstamp000000000000000000000004",
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = (DateTime?)null,
                    PhoneNumber = (string?)null
                },
                new
                {
                    Id = 5,
                    FirstName = "Eve",
                    LastName = "Customer",
                    Email = "customer2@gmail.com",
                    Username = "customer2",
                    PasswordHash = "Ov4LxpWKXXV9dwMYvBgqODdzIt0=", // Test123
                    PasswordSalt = "KtWF6g7SemBqs4nVWV4Ziw==",
                    IsActive = true,
                    SecurityStamp = "seedstamp000000000000000000000005",
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    LastLoginAt = (DateTime?)null,
                    PhoneNumber = (string?)null
                }
            );
        }

        private void SeedUserRoles(ModelBuilder modelBuilder)
        {
            // Map users to roles (UserRole has its own Id PK)
            // Admin role = RoleId 1, Customer role = RoleId 2
            modelBuilder.Entity<UserRole>().HasData(
                new
                {
                    Id = 1,
                    UserId = 1,
                    RoleId = 1,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 2,
                    UserId = 2,
                    RoleId = 1,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 3,
                    UserId = 3,
                    RoleId = 1,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 4,
                    UserId = 4,
                    RoleId = 2,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 5,
                    UserId = 5,
                    RoleId = 2,
                    DateAssigned = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        private void SeedAnimalCategories(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AnimalCategory>().HasData(
                new { Id = 1, Name = "Mammals" },
                new { Id = 2, Name = "Birds" },
                new { Id = 3, Name = "Reptiles" },
                new { Id = 4, Name = "Primates" }
            );
        }

        private void SeedAnimals(ModelBuilder modelBuilder)
        {
            var createdAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Animal>().HasData(
                new
                {
                    Id = 1,
                    Name = "Kavi",
                    Species = "Asian Elephant",
                    CategoryId = 1,
                    Habitat = "Tropical",
                    Diet = "Herbivore",
                    Weight = 4500m,
                    Lifespan = "60-70 years",
                    Description = "Kavi is a gentle 15-year-old Asian elephant rescued from a logging camp. He loves watermelons and mud baths.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.Adoptable,
                    IsActive = true,
                    AdoptionPrice = 15.00m,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 2,
                    Name = "Zara",
                    Species = "Red Panda",
                    CategoryId = 1,
                    Habitat = "Temperate",
                    Diet = "Omnivore",
                    Weight = 5.0m,
                    Lifespan = "8-12 years",
                    Description = "Zara is a playful red panda who spends her days napping in the treetops. She is especially fond of bamboo shoots and apple slices.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.Popular,
                    IsActive = true,
                    AdoptionPrice = (decimal?)null,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 3,
                    Name = "Milo",
                    Species = "Ring-tailed Lemur",
                    CategoryId = 4,
                    Habitat = "Tropical",
                    Diet = "Omnivore",
                    Weight = 2.5m,
                    Lifespan = "16-19 years",
                    Description = "Milo is a lively ring-tailed lemur who sunbathes with his troop each morning. He never misses a chance to steal a grape.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.New,
                    IsActive = true,
                    AdoptionPrice = (decimal?)null,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 4,
                    Name = "Luna",
                    Species = "Snow Leopard",
                    CategoryId = 1,
                    Habitat = "Mountain",
                    Diet = "Carnivore",
                    Weight = 40m,
                    Lifespan = "15-18 years",
                    Description = "Luna is a quiet snow leopard often seen stretching on cool rocky ledges at dusk. She is shy with crowds but very curious.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.Adoptable,
                    IsActive = true,
                    AdoptionPrice = 20.00m,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 5,
                    Name = "Bao",
                    Species = "Giant Panda",
                    CategoryId = 1,
                    Habitat = "Temperate",
                    Diet = "Herbivore",
                    Weight = 110m,
                    Lifespan = "20 years",
                    Description = "Bao is a calm giant panda with a famously picky appetite. His keepers know he will do almost anything for fresh bamboo.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.Popular,
                    IsActive = true,
                    AdoptionPrice = 25.00m,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 6,
                    Name = "Rio",
                    Species = "Scarlet Macaw",
                    CategoryId = 2,
                    Habitat = "Tropical",
                    Diet = "Omnivore",
                    Weight = 1.0m,
                    Lifespan = "40-50 years",
                    Description = "Rio is a brightly colored scarlet macaw who greets visitors with an impressive vocal range. He loves mango and showing off.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.New,
                    IsActive = true,
                    AdoptionPrice = (decimal?)null,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 7,
                    Name = "Nala",
                    Species = "African Lion",
                    CategoryId = 1,
                    Habitat = "Savanna",
                    Diet = "Carnivore",
                    Weight = 160m,
                    Lifespan = "10-14 years",
                    Description = "Nala is a confident lioness and one of the most popular residents of the savanna habitat. She spends warm afternoons dozing in the grass.",
                    ImageUrl = (string?)null,
                    Tag = eCommerce.Model.Enums.AnimalTag.Popular,
                    IsActive = true,
                    AdoptionPrice = 18.00m,
                    CreatedAt = createdAt
                }
            );
        }

        private void SeedActivityTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ActivityType>().HasData(
                new { Id = ActivityTypeSeedIds.Feeding, Name = "Feeding", Description = (string?)null },
                new { Id = ActivityTypeSeedIds.Safari, Name = "Safari", Description = (string?)null },
                new { Id = ActivityTypeSeedIds.Workshop, Name = "Workshop", Description = (string?)null },
                new { Id = ActivityTypeSeedIds.Experience, Name = "Experience", Description = (string?)null },
                new { Id = ActivityTypeSeedIds.Adventure, Name = "Adventure", Description = (string?)null }
            );
        }

        private void SeedActivities(ModelBuilder modelBuilder)
        {
            var createdAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Activity>().HasData(
                new
                {
                    Id = 1,
                    Name = "Elephant Feeding Experience",
                    Description = "Help keepers prepare fruit for Kavi and join a guided feeding session with our Asian elephant.",
                    AnimalId = 1,
                    ActivityTypeId = ActivityTypeSeedIds.Feeding,
                    Duration = new TimeSpan(1, 30, 0),
                    Price = 25.00m,
                    Capacity = 15,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 2,
                    Name = "Nocturnal Safari Walk",
                    Description = "A guided evening walk through the park to spot nocturnal animals as they become active.",
                    AnimalId = (int?)null,
                    ActivityTypeId = ActivityTypeSeedIds.Safari,
                    Duration = new TimeSpan(2, 0, 0),
                    Price = 30.00m,
                    Capacity = 20,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 3,
                    Name = "Bird Photography Workshop",
                    Description = "Learn composition and lighting tips while photographing macaws and other birds in the aviary.",
                    AnimalId = (int?)null,
                    ActivityTypeId = ActivityTypeSeedIds.Workshop,
                    Duration = new TimeSpan(2, 0, 0),
                    Price = 40.00m,
                    Capacity = 12,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 4,
                    Name = "Panda Keeper for a Day",
                    Description = "Spend a full day with keepers caring for Bao, from breakfast bamboo to enclosure enrichment.",
                    AnimalId = (int?)null,
                    ActivityTypeId = ActivityTypeSeedIds.Experience,
                    Duration = new TimeSpan(7, 0, 0),
                    Price = 120.00m,
                    Capacity = 4,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Draft,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 5,
                    Name = "Red Panda Snack Time",
                    Description = "Join keepers as they prepare fruit and bamboo snacks for Zara and watch her forage in the treetops.",
                    AnimalId = 2,
                    ActivityTypeId = ActivityTypeSeedIds.Feeding,
                    Duration = new TimeSpan(1, 0, 0),
                    Price = 20.00m,
                    Capacity = 12,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 6,
                    Name = "Lemur Morning Meet",
                    Description = "Meet Milo and his troop at sunrise, then help scatter grapes and browse during their morning forage.",
                    AnimalId = 3,
                    ActivityTypeId = ActivityTypeSeedIds.Experience,
                    Duration = new TimeSpan(1, 0, 0),
                    Price = 18.00m,
                    Capacity = 10,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 7,
                    Name = "Snow Leopard Twilight Safari",
                    Description = "A quiet late-afternoon walk to Luna's mountain habitat as she becomes active on the rocky ledges.",
                    AnimalId = 4,
                    ActivityTypeId = ActivityTypeSeedIds.Safari,
                    Duration = new TimeSpan(1, 30, 0),
                    Price = 35.00m,
                    Capacity = 16,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 8,
                    Name = "Lion Savanna Adventure",
                    Description = "Guided adventure through the savanna habitat with a close-up look at Nala and her pride's daily routine.",
                    AnimalId = 7,
                    ActivityTypeId = ActivityTypeSeedIds.Adventure,
                    Duration = new TimeSpan(2, 0, 0),
                    Price = 45.00m,
                    Capacity = 14,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 9,
                    Name = "Macaw Voice Workshop",
                    Description = "Learn how scarlet macaws communicate, then practice call-and-response with Rio in the aviary.",
                    AnimalId = 6,
                    ActivityTypeId = ActivityTypeSeedIds.Workshop,
                    Duration = new TimeSpan(1, 30, 0),
                    Price = 28.00m,
                    Capacity = 10,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 10,
                    Name = "Panda Enrichment Hour",
                    Description = "Help design and place puzzle feeders for Bao, then watch him work out how to reach the bamboo.",
                    AnimalId = 5,
                    ActivityTypeId = ActivityTypeSeedIds.Experience,
                    Duration = new TimeSpan(1, 0, 0),
                    Price = 32.00m,
                    Capacity = 8,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Active,
                    CreatedAt = createdAt
                }
            );
        }

        private void SeedSchedules(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Schedule>().HasData(
                new
                {
                    Id = 1,
                    ActivityId = 1,
                    Date = new DateOnly(2026, 9, 9),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 30),
                    CapacityLeft = 15,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 2,
                    ActivityId = 3,
                    Date = new DateOnly(2026, 9, 9),
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(13, 0),
                    CapacityLeft = 12,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 3,
                    ActivityId = 1,
                    Date = new DateOnly(2026, 9, 10),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 30),
                    CapacityLeft = 15,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 4,
                    ActivityId = 2,
                    Date = new DateOnly(2026, 9, 10),
                    StartTime = new TimeOnly(20, 0),
                    EndTime = new TimeOnly(22, 0),
                    CapacityLeft = 20,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 5,
                    ActivityId = 3,
                    Date = new DateOnly(2026, 9, 11),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(12, 0),
                    CapacityLeft = 12,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 6,
                    ActivityId = 1,
                    Date = new DateOnly(2026, 9, 12),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(15, 30),
                    CapacityLeft = 15,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 7,
                    ActivityId = 2,
                    Date = new DateOnly(2026, 9, 12),
                    StartTime = new TimeOnly(19, 30),
                    EndTime = new TimeOnly(21, 30),
                    CapacityLeft = 20,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 8,
                    ActivityId = 2,
                    Date = new DateOnly(2026, 9, 13),
                    StartTime = new TimeOnly(20, 0),
                    EndTime = new TimeOnly(22, 0),
                    CapacityLeft = 20,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 9,
                    ActivityId = 3,
                    Date = new DateOnly(2026, 9, 13),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(11, 0),
                    CapacityLeft = 12,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 10,
                    ActivityId = 5,
                    Date = new DateOnly(2026, 9, 14),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    CapacityLeft = 12,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 11,
                    ActivityId = 6,
                    Date = new DateOnly(2026, 9, 14),
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 0),
                    CapacityLeft = 10,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 12,
                    ActivityId = 7,
                    Date = new DateOnly(2026, 9, 14),
                    StartTime = new TimeOnly(17, 0),
                    EndTime = new TimeOnly(18, 30),
                    CapacityLeft = 16,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 13,
                    ActivityId = 8,
                    Date = new DateOnly(2026, 9, 15),
                    StartTime = new TimeOnly(11, 0),
                    EndTime = new TimeOnly(13, 0),
                    CapacityLeft = 14,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 14,
                    ActivityId = 9,
                    Date = new DateOnly(2026, 9, 15),
                    StartTime = new TimeOnly(14, 0),
                    EndTime = new TimeOnly(15, 30),
                    CapacityLeft = 10,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                },
                new
                {
                    Id = 15,
                    ActivityId = 10,
                    Date = new DateOnly(2026, 9, 16),
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 0),
                    CapacityLeft = 8,
                    Status = eCommerce.Model.Enums.ScheduleStatus.Active
                }
            );
        }

        private void SeedZooPartners(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ZooPartner>().HasData(
                new
                {
                    Id = 1,
                    Name = "Sunbird Sanctuary",
                    Location = "Costa Rica",
                    Description = (string?)null,
                    ImageUrl = (string?)null,
                    GoalAmount = 20000m,
                    CurrentAmount = 12400m,
                    IsActive = true
                },
                new
                {
                    Id = 2,
                    Name = "Mountain Haven Rescue",
                    Location = "Nepal",
                    Description = (string?)null,
                    ImageUrl = (string?)null,
                    GoalAmount = 15000m,
                    CurrentAmount = 8200m,
                    IsActive = true
                },
                new
                {
                    Id = 3,
                    Name = "Coral Coast Wildlife",
                    Location = "Australia",
                    Description = (string?)null,
                    ImageUrl = (string?)null,
                    GoalAmount = 25000m,
                    CurrentAmount = 18900m,
                    IsActive = true
                }
            );
        }

        private void SeedReviews(ModelBuilder modelBuilder)
        {
            var createdAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Review>().HasData(
                new
                {
                    Id = 1,
                    UserId = 3,
                    ActivityId = 1,
                    Rating = 5,
                    Comment = "Amazing experience! The kids were thrilled, staff were very kind.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 2,
                    UserId = 4,
                    ActivityId = 1,
                    Rating = 5,
                    Comment = "Kavi is such a gentle giant. Helping with the feeding was unforgettable.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 3,
                    UserId = 5,
                    ActivityId = 1,
                    Rating = 4,
                    Comment = "Really well organised. We learned a lot and the keepers were so patient.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 4,
                    UserId = 3,
                    ActivityId = 2,
                    Rating = 5,
                    Comment = "Magical evening walk. Spotting the nocturnal animals was a highlight of our trip.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 5,
                    UserId = 4,
                    ActivityId = 2,
                    Rating = 4,
                    Comment = "Loved the atmosphere after dark. Guides were knowledgeable and made everyone feel safe.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 6,
                    UserId = 5,
                    ActivityId = 2,
                    Rating = 5,
                    Comment = "A must-do! Quiet, atmospheric, and the kids stayed engaged the whole time.",
                    CreatedAt = createdAt
                },
                new
                {
                    Id = 7,
                    UserId = 3,
                    ActivityId = 3,
                    Rating = 2,
                    Comment = "Terrible experience. The guide was rude and we waited far too long.",
                    CreatedAt = new DateTime(2025, 10, 12, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 8,
                    UserId = 4,
                    ActivityId = 3,
                    Rating = 3,
                    Comment = "The session starts at 10:00 and lasts about one hour.",
                    CreatedAt = new DateTime(2025, 11, 4, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 9,
                    UserId = 5,
                    ActivityId = 3,
                    Rating = 5,
                    Comment = "Wonderful morning. The keepers were friendly and I would recommend this activity.",
                    CreatedAt = new DateTime(2025, 12, 16, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 10,
                    UserId = 3,
                    ActivityId = 4,
                    Rating = 1,
                    Comment = "Awful organisation. The enclosure was dirty and the whole visit felt wasted.",
                    CreatedAt = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 11,
                    UserId = 4,
                    ActivityId = 4,
                    Rating = 3,
                    Comment = "Parking is beside the main gate and tickets are checked at the entrance.",
                    CreatedAt = new DateTime(2026, 2, 11, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 12,
                    UserId = 5,
                    ActivityId = 4,
                    Rating = 5,
                    Comment = "Excellent activity. Everything was well organised and the kids were thrilled.",
                    CreatedAt = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 13,
                    UserId = 4,
                    ActivityId = 6,
                    Rating = 2,
                    Comment = "Disappointing tour. The staff were unhelpful and this was the worst workshop.",
                    CreatedAt = new DateTime(2025, 10, 21, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 14,
                    UserId = 5,
                    ActivityId = 6,
                    Rating = 3,
                    Comment = "The workshop is listed on the Tuesday schedule and lasts ninety minutes.",
                    CreatedAt = new DateTime(2026, 1, 19, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 15,
                    UserId = 3,
                    ActivityId = 5,
                    Rating = 4,
                    Comment = "We loved this safari. It was amazing and I recommend it to every family.",
                    CreatedAt = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 16,
                    UserId = 5,
                    ActivityId = 7,
                    Rating = 1,
                    Comment = "The worst feeding we have had. Staff were rude and the delay was awful.",
                    CreatedAt = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 17,
                    UserId = 4,
                    ActivityId = 7,
                    Rating = 3,
                    Comment = "Tickets are checked at the entrance and the tour starts at the main gate.",
                    CreatedAt = new DateTime(2026, 4, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 18,
                    UserId = 3,
                    ActivityId = 6,
                    Rating = 5,
                    Comment = "A wonderful workshop. The guide was excellent and we loved every minute.",
                    CreatedAt = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }

        private void SeedBadges(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Badge>().HasData(
                new
                {
                    Id = 1,
                    Name = "Wildlife Guardian",
                    Description = "Adopt your first animal",
                    IconUrl = (string?)null,
                    Criteria = eCommerce.Model.Enums.BadgeCriteria.FirstAdoption
                },
                new
                {
                    Id = 2,
                    Name = "First Explorer",
                    Description = "Book your first activity",
                    IconUrl = (string?)null,
                    Criteria = eCommerce.Model.Enums.BadgeCriteria.FirstBooking
                },
                new
                {
                    Id = 3,
                    Name = "Generous Heart",
                    Description = "Donate to 3 zoos",
                    IconUrl = (string?)null,
                    Criteria = eCommerce.Model.Enums.BadgeCriteria.ThreeDonations
                },
                new
                {
                    Id = 5,
                    Name = "Conservation Hero",
                    Description = "Adopt 5 animals",
                    IconUrl = (string?)null,
                    Criteria = eCommerce.Model.Enums.BadgeCriteria.FiveAdoptions
                }
            );
        }

        private void SeedNews(ModelBuilder modelBuilder)
        {
            // Alice Admin (user 1) is the author. The last row stays unpublished so visitors do not see a draft.
            modelBuilder.Entity<NewsItem>().HasData(
                new
                {
                    Id = 1,
                    Title = "Night safari walks return this weekend",
                    Text = "After-dark walks with the night animals are open again this weekend. Places are limited, so reserve a schedule in the app before you arrive.",
                    ImageUrl = global::eCommerce.Services.Images.NewsSeedImages.SafariUrl,
                    PublishedAt = new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc),
                    AuthorUserId = 1,
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 10, 12, 18, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 2,
                    Title = "Meet the new red panda twins",
                    Text = "Two red panda twins are now on view in the forest habitat. Keep a little distance, and follow the keeper's notes posted by the enclosure.",
                    ImageUrl = global::eCommerce.Services.Images.NewsSeedImages.PandaUrl,
                    PublishedAt = new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc),
                    AuthorUserId = 1,
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 3,
                    Title = "Spring habitat renewal is complete",
                    Text = "The spring planting in the wetland habitat is finished. New shade and water plants are in place for the warmer months.",
                    ImageUrl = global::eCommerce.Services.Images.NewsSeedImages.HabitatUrl,
                    PublishedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                    AuthorUserId = 1,
                    IsPublished = true,
                    CreatedAt = new DateTime(2026, 4, 2, 9, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 4,
                    Title = "Winter lecture series outline",
                    Text = "A draft outline for the winter lectures. This item stays unpublished until the dates are confirmed.",
                    ImageUrl = global::eCommerce.Services.Images.NewsSeedImages.DraftUrl,
                    PublishedAt = new DateTime(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc),
                    AuthorUserId = 1,
                    IsPublished = false,
                    CreatedAt = new DateTime(2026, 10, 19, 8, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                }
            );
        }
    }
}
