using eCommerce.Services.ProductStateMachine;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Database
{
    public partial class ECommerceDbContext : DbContext
    {
        private void CreateSeed(ModelBuilder modelBuilder)
        {
            SeedProductTypes(modelBuilder);
            SeedUnitsOfMeasure(modelBuilder);
            SeedCategories(modelBuilder);
            SeedProducts(modelBuilder);
            SeedProductCategories(modelBuilder);
            SeedRoles(modelBuilder);
            SeedUsers(modelBuilder);
            SeedUserRoles(modelBuilder);
            SeedAnimalCategories(modelBuilder);
            SeedAnimals(modelBuilder);
            SeedActivities(modelBuilder);
            SeedSchedules(modelBuilder);
            SeedZooPartners(modelBuilder);
        }

        private void SeedProductTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductType>().HasData(
                new
                {
                    Id = 1,
                    Name = "Physical",
                    Description = "Tangible products that require shipping",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 2,
                    Name = "Digital",
                    Description = "Intangible products that can be downloaded",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 3,
                    Name = "Service",
                    Description = "Non-physical products that provide a service",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                }
            );
        }

        private void SeedUnitsOfMeasure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UnitOfMeasure>().HasData(
               new
               {
                   Id = 1,
                   Name = "Piece",
                   Abbreviation = "pc",
                   IsActive = true,
                   Description = "",
                   CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                   UpdatedAt = (DateTime?)null
               },
               new
               {
                   Id = 2,
                   Name = "Kilogram",
                   Abbreviation = "kg",
                   Description = "",
                   IsActive = true,
                   CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                   UpdatedAt = (DateTime?)null
               },
               new
               {
                   Id = 3,
                   Name = "Liter",
                   Abbreviation = "L",
                   Description = "",
                   IsActive = true,
                   CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                   UpdatedAt = (DateTime?)null
               });
        }

        private void SeedCategories(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new
                {
                    Id = 1,
                    Name = "Electronics",
                    Description = "Electronic devices and accessories",
                    ParentCategoryId = (int?)null,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 2,
                    Name = "Computers",
                    Description = "Desktops, laptops and related hardware",
                    ParentCategoryId = (int?)1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 3,
                    Name = "Mobile Phones",
                    Description = "Smartphones and mobile devices",
                    ParentCategoryId = (int?)1,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                },
                new
                {
                    Id = 4,
                    Name = "Accessories",
                    Description = "Device accessories and peripherals",
                    ParentCategoryId = (int?)null,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null
                }
            );
        }

        private void SeedProducts(ModelBuilder modelBuilder)
        {
            // Seed Products
            modelBuilder.Entity<Product>().HasData(
                new
                {
                    Id = 1,
                    Name = "Gaming Laptop",
                    Description = "High-performance laptop suitable for gaming and development",
                    Price = 999.99m,
                    StockQuantity = 10,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "LAP-1000",
                    Weight = 2500m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 2,
                    Name = "Smartphone X",
                    Description = "Latest generation smartphone with advanced camera features",
                    Price = 699.99m,
                    StockQuantity = 25,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "PHN-2000",
                    Weight = 180m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 3,
                    Name = "Wireless Mouse",
                    Description = "Ergonomic wireless mouse with long battery life",
                    Price = 19.99m,
                    StockQuantity = 150,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "MSE-300",
                    Weight = 100m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 4,
                    Name = "USB-C Fast Charger",
                    Description = "65W USB-C fast charger compatible with laptops and phones",
                    Price = 29.99m,
                    StockQuantity = 200,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "CHR-400",
                    Weight = 120m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 5,
                    Name = "Mechanical Keyboard",
                    Description = "RGB mechanical keyboard with tactile switches",
                    Price = 89.99m,
                    StockQuantity = 75,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "KEY-500",
                    Weight = 900m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 6,
                    Name = "Noise-Cancelling Headphones",
                    Description = "Over-ear headphones with active noise cancellation",
                    Price = 199.99m,
                    StockQuantity = 40,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "HDP-600",
                    Weight = 350m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 7,
                    Name = "27\" 4K Monitor",
                    Description = "27-inch 4K UHD monitor with HDR and low response time",
                    Price = 349.99m,
                    StockQuantity = 30,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "MON-700",
                    Weight = 4500m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                 new
                 {
                     Id = 8,
                     Name = "External SSD 1TB",
                     Description = "Portable 1TB external SSD with high-speed USB-C connectivity",
                     Price = 129.99m,
                     StockQuantity = 60,
                     IsActive = true,
                     CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                     UpdatedAt = (DateTime?)null,
                     SKU = "SSD-800",
                     Weight = 80m,
                     ProductTypeId = (int?)null,
                     UnitOfMeasureId = (int?)null,
                     ProductState = nameof(DraftProductState)
                 },
                new
                {
                    Id = 9,
                    Name = "Webcam Pro 1080p",
                    Description = "Full HD webcam with built-in microphone and privacy shutter",
                    Price = 59.99m,
                    StockQuantity = 90,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "CAM-900",
                    Weight = 140m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 10,
                    Name = "Bluetooth Speaker",
                    Description = "Compact Bluetooth speaker with waterproof design and deep bass",
                    Price = 49.99m,
                    StockQuantity = 110,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "SPK-1000",
                    Weight = 620m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 11,
                    Name = "Smartwatch Active",
                    Description = "Fitness-focused smartwatch with heart-rate tracking and GPS",
                    Price = 149.99m,
                    StockQuantity = 55,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "WCH-1100",
                    Weight = 50m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 12,
                    Name = "Laptop Stand Aluminum",
                    Description = "Adjustable aluminum laptop stand for improved desk ergonomics",
                    Price = 39.99m,
                    StockQuantity = 85,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "STD-1200",
                    Weight = 750m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 13,
                    Name = "Wi-Fi 6 Router",
                    Description = "Dual-band Wi-Fi 6 router with extended coverage for home networks",
                    Price = 119.99m,
                    StockQuantity = 45,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "RTR-1300",
                    Weight = 680m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 14,
                    Name = "Graphics Tablet",
                    Description = "Digital drawing tablet with pressure-sensitive stylus",
                    Price = 79.99m,
                    StockQuantity = 35,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "TAB-1400",
                    Weight = 420m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 15,
                    Name = "Portable Power Bank",
                    Description = "20000mAh portable power bank with dual USB output",
                    Price = 34.99m,
                    StockQuantity = 130,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "PWR-1500",
                    Weight = 410m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 16,
                    Name = "Ethernet Cable 10m",
                    Description = "Cat 6 Ethernet cable for reliable high-speed wired networking",
                    Price = 12.99m,
                    StockQuantity = 300,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "NET-1600",
                    Weight = 260m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 17,
                    Name = "Docking Station",
                    Description = "USB-C docking station with HDMI, Ethernet, USB-A, and card reader ports",
                    Price = 99.99m,
                    StockQuantity = 50,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "DOC-1700",
                    Weight = 520m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 18,
                    Name = "Smart LED Desk Lamp",
                    Description = "Adjustable LED desk lamp with touch controls and multiple brightness levels",
                    Price = 44.99m,
                    StockQuantity = 70,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "LMP-1800",
                    Weight = 850m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 19,
                    Name = "Action Camera",
                    Description = "Compact 4K action camera with waterproof casing and image stabilization",
                    Price = 179.99m,
                    StockQuantity = 28,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "ACT-1900",
                    Weight = 160m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 20,
                    Name = "VR Headset",
                    Description = "Standalone virtual reality headset with motion controllers",
                    Price = 299.99m,
                    StockQuantity = 20,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "VRH-2000",
                    Weight = 620m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 21,
                    Name = "Smart Home Hub",
                    Description = "Central smart home hub for connecting lights, sensors, and voice assistants",
                    Price = 84.99m,
                    StockQuantity = 42,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "HUB-2100",
                    Weight = 300m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 22,
                    Name = "Fitness Tracker Band",
                    Description = "Lightweight fitness tracker with step counting, sleep monitoring, and notifications",
                    Price = 69.99m,
                    StockQuantity = 95,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "FIT-2200",
                    Weight = 35m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 23,
                    Name = "Wireless Charging Pad",
                    Description = "Slim wireless charging pad compatible with Qi-enabled smartphones and earbuds",
                    Price = 24.99m,
                    StockQuantity = 160,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "WCP-2300",
                    Weight = 110m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 24,
                    Name = "NAS Storage Enclosure",
                    Description = "Two-bay network attached storage enclosure for backups and media sharing",
                    Price = 229.99m,
                    StockQuantity = 18,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "NAS-2400",
                    Weight = 1300m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 25,
                    Name = "Digital Voice Recorder",
                    Description = "Portable digital voice recorder with noise reduction and long recording time",
                    Price = 54.99m,
                    StockQuantity = 65,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "REC-2500",
                    Weight = 90m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 26,
                    Name = "Mini Projector",
                    Description = "Portable mini projector with HDMI input and built-in speaker",
                    Price = 159.99m,
                    StockQuantity = 32,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "PRJ-2600",
                    Weight = 950m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                },
                new
                {
                    Id = 27,
                    Name = "Smart Doorbell Camera",
                    Description = "Wi-Fi smart doorbell camera with motion detection and two-way audio",
                    Price = 139.99m,
                    StockQuantity = 38,
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = (DateTime?)null,
                    SKU = "DRB-2700",
                    Weight = 250m,
                    ProductTypeId = (int?)null,
                    UnitOfMeasureId = (int?)null,
                    ProductState = nameof(DraftProductState)
                }
            );
        }

        private void SeedProductCategories(ModelBuilder modelBuilder)
        {
            // Seed ProductCategory relationships
            // ProductCategory has its own Id primary key; provide deterministic Ids
            modelBuilder.Entity<ProductCategory>().HasData(
                new
                {
                    Id = 1,
                    ProductId = 1, // Gaming Laptop
                    CategoryId = 2  // Computers
                },
                new
                {
                    Id = 2,
                    ProductId = 2, // Smartphone X
                    CategoryId = 3  // Mobile Phones
                },
                new
                {
                    Id = 3,
                    ProductId = 3, // Wireless Mouse
                    CategoryId = 4  // Accessories
                },
                new
                {
                    Id = 4,
                    ProductId = 4, // USB-C Fast Charger
                    CategoryId = 4  // Accessories
                },
                new
                {
                    Id = 5,
                    ProductId = 5, // Mechanical Keyboard
                    CategoryId = 2  // Computers
                },
                new
                {
                    Id = 6,
                    ProductId = 6, // Noise-Cancelling Headphones
                    CategoryId = 1  // Electronics
                },
                new
                {
                    Id = 7,
                    ProductId = 7, // 27" 4K Monitor
                    CategoryId = 2  // Computers
                }
            );
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            // Seed Roles - deterministic Ids: 1 = Admin, 2 = Customer
            modelBuilder.Entity<Role>().HasData(
                new
                {
                    Id = 1,
                    Name = "Admin",
                    Description = "Administrator role with full permissions",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc)
                },
                new
                {
                    Id = 2,
                    Name = "Customer",
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
                    Type = eCommerce.Model.Enums.ActivityType.Feeding,
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
                    Type = eCommerce.Model.Enums.ActivityType.Safari,
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
                    Type = eCommerce.Model.Enums.ActivityType.Workshop,
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
                    Type = eCommerce.Model.Enums.ActivityType.Experience,
                    Duration = new TimeSpan(7, 0, 0),
                    Price = 120.00m,
                    Capacity = 4,
                    ImageUrl = (string?)null,
                    Status = eCommerce.Model.Enums.ActivityStatus.Draft,
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
    }
}
