using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
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
                new SilentLoggerFactory());
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
