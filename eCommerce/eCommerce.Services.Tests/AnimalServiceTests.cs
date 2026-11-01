using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AnimalServiceTests
    {
        [Fact]
        public async Task List_CountsOnlyActiveAdoptions()
        {
            using var db = new SqliteDb();
            db.Context.Adoptions.AddRange(
                Adoption(4, 1, AdoptionStatus.Active),
                Adoption(5, 1, AdoptionStatus.Cancelled),
                Adoption(4, 1, AdoptionStatus.PendingPayment),
                Adoption(4, 7, AdoptionStatus.Active));
            await db.Context.SaveChangesAsync();

            var page = await Animals(db.Context).GetAllAsync(new AnimalSearchObject
            {
                Page = 1,
                PageSize = 100,
                IncludeTotalCount = true
            });

            Assert.Equal(1, page.Items.Single(animal => animal.Name == "Kavi").ActiveAdoptionCount);
            Assert.Equal(1, page.Items.Single(animal => animal.Name == "Nala").ActiveAdoptionCount);
            Assert.Equal(0, page.Items.Single(animal => animal.Name == "Zara").ActiveAdoptionCount);
        }

        [Fact]
        public async Task NameSearch_MatchesName_AndLeavesSpeciesSearchOnFts()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);

            var byName = await service.GetAllAsync(new AnimalSearchObject { Name = "Lion", PageSize = 100 });
            Assert.Empty(byName.Items);

            var nala = await service.GetAllAsync(new AnimalSearchObject { Name = "Nala", PageSize = 100 });
            Assert.Equal("Nala", Assert.Single(nala.Items).Name);

            var bySpecies = await service.GetAllAsync(new AnimalSearchObject { FTS = "Lion", PageSize = 100 });
            Assert.Equal("Nala", Assert.Single(bySpecies.Items).Name);
        }

        [Fact]
        public async Task Habitat_LongerThan100Characters_IsRejected()
        {
            using var db = new SqliteDb();
            var error = await Assert.ThrowsAsync<ValidationException>(() => Animals(db.Context).InsertAsync(new AnimalInsertRequest
            {
                Name = "Temp",
                Species = "Test species",
                CategoryId = 1,
                Habitat = new string('a', 101)
            }));

            Assert.Contains(error.Errors, failure => failure.ErrorMessage == "Habitat cannot exceed 100 characters.");
        }

        [Fact]
        public async Task Delete_WithoutDependents_RemovesTheAnimal()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(NewAnimal("Temp"));

            await service.DeleteAsync(created.Id);

            Assert.False(await db.Context.Animals.AnyAsync(animal => animal.Id == created.Id));
        }

        [Fact]
        public async Task Delete_BlockedByDraftActivity_NamesTheCount()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(NewAnimal("Draft host"));
            db.Context.Activities.Add(LinkedActivity(created.Id, "Quiet hour", ActivityStatus.Draft));
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(created.Id));

            Assert.Equal("This animal is used by 1 activity and cannot be deleted.", error.Message);
            Assert.True(await db.Context.Animals.AnyAsync(animal => animal.Id == created.Id));
        }

        [Fact]
        public async Task Delete_BlockedByPendingUnpaidAdoption_NamesTheCount()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(NewAnimal("Pending host"));
            db.Context.Adoptions.Add(Adoption(4, created.Id, AdoptionStatus.PendingPayment));
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(created.Id));

            Assert.Equal("This animal is used by 1 adoption and cannot be deleted.", error.Message);
            Assert.True(await db.Context.Animals.AnyAsync(animal => animal.Id == created.Id));
        }

        [Fact]
        public async Task Delete_BlockedByActivitiesAndAdoptions_NamesBothCounts()
        {
            using var db = new SqliteDb();
            var service = Animals(db.Context);
            var created = await service.InsertAsync(NewAnimal("Busy host"));
            db.Context.Activities.AddRange(
                LinkedActivity(created.Id, "Morning draft", ActivityStatus.Draft),
                LinkedActivity(created.Id, "Closed tour", ActivityStatus.Inactive));
            db.Context.Adoptions.AddRange(
                Adoption(4, created.Id, AdoptionStatus.PendingPayment),
                Adoption(5, created.Id, AdoptionStatus.Cancelled));
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(created.Id));

            Assert.Equal(
                "This animal is used by 2 activities and 2 adoptions and cannot be deleted.",
                error.Message);
            Assert.True(await db.Context.Animals.AnyAsync(animal => animal.Id == created.Id));
        }

        [Fact]
        public async Task Seed_DoesNotIncludeAnimalWhisperer()
        {
            using var db = new SqliteDb();
            var names = await db.Context.Badges.Select(badge => badge.Name).ToListAsync();

            Assert.DoesNotContain("Animal Whisperer", names);
            Assert.DoesNotContain(Enum.GetNames<BadgeCriteria>(), name => name == "TenAnimalVisits");
            Assert.Equal(4, await db.Context.Badges.CountAsync());
        }

        private static AnimalService Animals(ECommerceDbContext context)
        {
            return new AnimalService(
                context,
                new Mapper(),
                new AnimalInsertValidator(),
                new AnimalUpdateValidator(),
                new ZooClock("Europe/Sarajevo"),
                new UnusedImages(),
                new SilentLoggerFactory());
        }

        private sealed class UnusedImages : IStoredImageService
        {
            public string Save(byte[] content) => throw new InvalidOperationException("This test does not upload an image.");

            public void Delete(string? relativeUrl)
            {
            }

            public StoredImageFile? Read(string fileName) => null;
        }

        private static AnimalInsertRequest NewAnimal(string name)
        {
            return new AnimalInsertRequest
            {
                Name = name,
                Species = "Test species",
                CategoryId = 1
            };
        }

        private static Activity LinkedActivity(int animalId, string name, ActivityStatus status)
        {
            return new Activity
            {
                Name = name,
                AnimalId = animalId,
                ActivityTypeId = 1,
                Duration = TimeSpan.FromHours(1),
                Price = 10m,
                Capacity = 8,
                Status = status
            };
        }

        private static Adoption Adoption(int userId, int animalId, AdoptionStatus status)
        {
            return new Adoption
            {
                UserId = userId,
                AnimalId = animalId,
                MonthlyAmount = 10,
                Status = status,
                StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)
            };
        }
    }
}
