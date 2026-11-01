using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services.ActivityStateMachine;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class CatalogImageTests : IDisposable
    {
        private readonly string _uploads = Path.Combine(Path.GetTempPath(), "wildnest-catalog-tests", Guid.NewGuid().ToString("N"));

        public CatalogImageTests()
        {
            Directory.CreateDirectory(_uploads);
        }

        public void Dispose()
        {
            if (Directory.Exists(_uploads))
            {
                Directory.Delete(_uploads, recursive: true);
            }
        }

        [Fact]
        public async Task AnimalInsert_StoresAnUploadedImageAsAServerPath()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);

            var created = await service.InsertAsync(AnimalRequest(
                imageBase64: Png(),
                imageUrl: "https://example.com/typed-animal.png"));

            Assert.StartsWith("/media/", created.ImageUrl);
            Assert.NotEqual("https://example.com/typed-animal.png", created.ImageUrl);
            Assert.True(File.Exists(StoredFile(created.ImageUrl!)));
        }

        [Fact]
        public async Task AnimalInsert_WithOnlyATypedUrl_DoesNotStoreThatPath()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);

            var created = await service.InsertAsync(AnimalRequest(imageUrl: "https://example.com/typed-animal.png"));

            Assert.Null(created.ImageUrl);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        [Fact]
        public async Task AnimalUpdate_WithoutANewFile_KeepsTheStoredPath()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(AnimalRequest(imageBase64: Png()));
            db.Context.ChangeTracker.Clear();

            var updated = await service.UpdateAsync(created.Id, AnimalUpdate(imageUrl: "https://example.com/replacement.png"));

            Assert.Equal(created.ImageUrl, updated.ImageUrl);
            Assert.True(File.Exists(StoredFile(updated.ImageUrl!)));
        }

        [Fact]
        public async Task AnimalUpdate_WithoutANewFile_KeepsASeededPath()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);

            var updated = await service.UpdateAsync(1, AnimalUpdate(
                name: "Kavi",
                imageUrl: "https://example.com/not-uploaded.png"));

            Assert.Equal(SeedImages.AnimalKavi, updated.ImageUrl);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        [Fact]
        public async Task AnimalUpdate_ReplacesTheFile_AndClearDeletesIt()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(AnimalRequest(imageBase64: Png(1, 2, 3)));
            var firstFile = StoredFile(created.ImageUrl!);
            db.Context.ChangeTracker.Clear();

            var replaced = await service.UpdateAsync(created.Id, AnimalUpdate(imageBase64: Png(4, 5, 6)));

            Assert.NotEqual(created.ImageUrl, replaced.ImageUrl);
            Assert.StartsWith("/media/", replaced.ImageUrl);
            Assert.False(File.Exists(firstFile));
            Assert.True(File.Exists(StoredFile(replaced.ImageUrl!)));
            db.Context.ChangeTracker.Clear();

            var cleared = await service.UpdateAsync(replaced.Id, AnimalUpdate(imageUrl: ""));

            Assert.Null(cleared.ImageUrl);
            Assert.False(File.Exists(StoredFile(replaced.ImageUrl!)));
        }

        [Fact]
        public async Task AnimalInsert_RejectsANonImage()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(AnimalRequest(
                imageBase64: Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
                contentType: "image/gif")));

            Assert.Equal(ImageContent.InvalidMessage, error.Message);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
            Assert.False(await db.Context.Animals.AnyAsync(animal => animal.Name == "Temp animal"));
        }

        [Fact]
        public async Task ActivityInsert_StoresAnUploadedImageAsAServerPath()
        {
            using var db = new SqliteDb();
            var service = Activities(db.Context);

            var created = await service.InsertAsync(ActivityRequest(
                imageBase64: Png(),
                imageUrl: "https://example.com/typed-activity.png"));

            Assert.StartsWith("/media/", created.ImageUrl);
            Assert.NotEqual("https://example.com/typed-activity.png", created.ImageUrl);
            Assert.Equal(ActivityStatus.Draft, created.Status);
            Assert.True(File.Exists(StoredFile(created.ImageUrl!)));
        }

        [Fact]
        public async Task ActivityUpdate_WithoutANewFile_KeepsTheStoredPath()
        {
            using var db = new SqliteDb();
            var service = Activities(db.Context);
            var created = await service.InsertAsync(ActivityRequest(imageBase64: Png()));
            db.Context.ChangeTracker.Clear();

            var updated = await service.UpdateAsync(created.Id, ActivityUpdate(imageUrl: "https://example.com/replacement.png"));

            Assert.Equal("Keeper talk revised", updated.Name);
            Assert.Equal(created.ImageUrl, updated.ImageUrl);
            Assert.True(File.Exists(StoredFile(updated.ImageUrl!)));
        }

        [Fact]
        public async Task ActivityUpdate_ReplacesTheFile_AndClearDeletesIt()
        {
            using var db = new SqliteDb();
            var service = Activities(db.Context);
            var created = await service.InsertAsync(ActivityRequest(imageBase64: Png(1, 2, 3)));
            var firstFile = StoredFile(created.ImageUrl!);
            db.Context.ChangeTracker.Clear();

            var replaced = await service.UpdateAsync(created.Id, ActivityUpdate(imageBase64: Png(4, 5, 6)));

            Assert.NotEqual(created.ImageUrl, replaced.ImageUrl);
            Assert.StartsWith("/media/", replaced.ImageUrl);
            Assert.False(File.Exists(firstFile));
            Assert.True(File.Exists(StoredFile(replaced.ImageUrl!)));
            db.Context.ChangeTracker.Clear();

            var cleared = await service.UpdateAsync(replaced.Id, ActivityUpdate(imageUrl: ""));

            Assert.Null(cleared.ImageUrl);
            Assert.False(File.Exists(StoredFile(replaced.ImageUrl!)));
        }

        [Fact]
        public async Task ActivityUpdate_WithoutANewFile_KeepsASeededPath()
        {
            using var db = new SqliteDb();
            var service = Activities(db.Context);
            var seeded = await db.Context.Activities.SingleAsync(activity => activity.Id == 1);
            seeded.Status = ActivityStatus.Draft;
            await db.Context.SaveChangesAsync();
            db.Context.ChangeTracker.Clear();

            var updated = await service.UpdateAsync(1, new ActivityUpdateRequest
            {
                Name = "Elephant Feeding Experience",
                Description = seeded.Description,
                AnimalId = seeded.AnimalId,
                ActivityTypeId = seeded.ActivityTypeId,
                Duration = seeded.Duration,
                Price = seeded.Price,
                Capacity = seeded.Capacity,
                ImageUrl = "https://example.com/not-uploaded.png"
            });

            Assert.Equal(SeedImages.ActivityElephantFeeding, updated.ImageUrl);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        [Fact]
        public async Task ActivityInsert_RejectsANonImage()
        {
            using var db = new SqliteDb();
            var service = Activities(db.Context);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(ActivityRequest(
                imageBase64: Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
                contentType: "image/gif")));

            Assert.Equal(ImageContent.InvalidMessage, error.Message);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
            Assert.False(await db.Context.Activities.AnyAsync(activity => activity.Name == "Keeper talk"));
        }

        private AnimalService Animals(ECommerceDbContext context)
        {
            return new AnimalService(
                context,
                new Mapper(),
                new AnimalInsertValidator(),
                new AnimalUpdateValidator(),
                new ZooClock("Europe/Sarajevo"),
                Images(),
                new SilentLoggerFactory());
        }

        private ActivityService Activities(ECommerceDbContext context)
        {
            var loggerFactory = new SilentLoggerFactory();
            var mapper = new Mapper();
            var provider = new StateBag();
            var initial = new InitialActivityState(context, mapper, provider, loggerFactory);
            var draft = new DraftActivityState(context, mapper, provider, loggerFactory);
            var active = new ActiveActivityState(context, mapper, provider, loggerFactory);
            var inactive = new InactiveActivityState(context, mapper, provider, loggerFactory);
            provider.Add(typeof(InitialActivityState), initial);
            provider.Add(typeof(DraftActivityState), draft);
            provider.Add(typeof(ActiveActivityState), active);
            provider.Add(typeof(InactiveActivityState), inactive);

            return new ActivityService(
                context,
                mapper,
                new BaseActivityState(context, mapper, provider, loggerFactory),
                new TestUserAccessor { Admin = true, UserId = 1 },
                new ActivityInsertValidator(),
                new ActivityUpdateValidator(),
                Images(),
                loggerFactory);
        }

        private FileStoredImageService Images() => new(new UploadSettings(_uploads));

        private string StoredFile(string url) => Path.Combine(_uploads, url["/media/".Length..]);

        private static string Png(byte red = 10, byte green = 20, byte blue = 30)
        {
            return Convert.ToBase64String(NewsSeedImages.SolidPng(red, green, blue));
        }

        private static AnimalInsertRequest AnimalRequest(string? imageBase64 = null, string? imageUrl = null, string contentType = "image/png")
        {
            return new AnimalInsertRequest
            {
                Name = "Temp animal",
                Species = "Test species",
                CategoryId = 1,
                ImageUrl = imageUrl,
                ImageBase64 = imageBase64,
                ImageContentType = imageBase64 == null ? null : contentType
            };
        }

        private static AnimalUpdateRequest AnimalUpdate(string name = "Temp animal", string? imageBase64 = null, string? imageUrl = null)
        {
            return new AnimalUpdateRequest
            {
                Name = name,
                Species = "Test species",
                CategoryId = 1,
                ImageUrl = imageUrl,
                ImageBase64 = imageBase64,
                ImageContentType = imageBase64 == null ? null : "image/png"
            };
        }

        private static ActivityInsertRequest ActivityRequest(string? imageBase64 = null, string? imageUrl = null, string contentType = "image/png")
        {
            return new ActivityInsertRequest
            {
                Name = "Keeper talk",
                Description = "A short talk beside the habitat.",
                ActivityTypeId = ActivityTypeSeedIds.Workshop,
                Duration = TimeSpan.FromMinutes(45),
                Price = 18,
                Capacity = 12,
                ImageUrl = imageUrl,
                ImageBase64 = imageBase64,
                ImageContentType = imageBase64 == null ? null : contentType
            };
        }

        private static ActivityUpdateRequest ActivityUpdate(string? imageBase64 = null, string? imageUrl = null)
        {
            return new ActivityUpdateRequest
            {
                Name = "Keeper talk revised",
                Description = "The talk moves to the afternoon.",
                ActivityTypeId = ActivityTypeSeedIds.Workshop,
                Duration = TimeSpan.FromMinutes(45),
                Price = 18,
                Capacity = 12,
                ImageUrl = imageUrl,
                ImageBase64 = imageBase64,
                ImageContentType = imageBase64 == null ? null : "image/png"
            };
        }
    }
}
