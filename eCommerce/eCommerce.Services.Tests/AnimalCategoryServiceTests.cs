using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using eCommerce.Services.Validators;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AnimalCategoryServiceTests
    {
        [Fact]
        public async Task Delete_WithoutAnimals_RemovesTheCategory()
        {
            using var db = new SqliteDb();
            var service = Categories(db.Context);
            var created = await service.InsertAsync(new AnimalCategoryInsertRequest { Name = "Amphibians" });

            await service.DeleteAsync(created.Id);

            Assert.False(await db.Context.AnimalCategories.AnyAsync(category => category.Id == created.Id));
        }

        [Fact]
        public async Task Delete_UsedByOneAnimal_NamesTheCount()
        {
            using var db = new SqliteDb();
            var service = Categories(db.Context);
            var created = await service.InsertAsync(new AnimalCategoryInsertRequest { Name = "Fish" });
            db.Context.Animals.Add(LinkedAnimal(created.Id, "Finn", active: true));
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(created.Id));

            Assert.Equal("This category is used by 1 animal and cannot be deleted.", error.Message);
            Assert.True(await db.Context.AnimalCategories.AnyAsync(category => category.Id == created.Id));
        }

        [Fact]
        public async Task Delete_UsedByThreeAnimals_NamesTheCount_IncludingInactive()
        {
            using var db = new SqliteDb();
            var service = Categories(db.Context);
            var created = await service.InsertAsync(new AnimalCategoryInsertRequest { Name = "Insects" });
            db.Context.Animals.AddRange(
                LinkedAnimal(created.Id, "Ada", active: true),
                LinkedAnimal(created.Id, "Bea", active: false),
                LinkedAnimal(created.Id, "Cleo", active: true));
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(created.Id));

            Assert.Equal("This category is used by 3 animals and cannot be deleted.", error.Message);
            Assert.True(await db.Context.AnimalCategories.AnyAsync(category => category.Id == created.Id));
            Assert.Equal(3, await db.Context.Animals.CountAsync(animal => animal.CategoryId == created.Id));
        }

        [Fact]
        public async Task Delete_MissingCategory_IsNotFound()
        {
            using var db = new SqliteDb();
            var error = await Assert.ThrowsAsync<NotFoundException>(() => Categories(db.Context).DeleteAsync(99999));

            Assert.Equal("Category not found.", error.Message);
        }

        private static AnimalCategoryService Categories(ECommerceDbContext context)
        {
            return new AnimalCategoryService(
                context,
                new Mapper(),
                new AnimalCategoryInsertValidator(),
                new AnimalCategoryUpdateValidator(),
                new SilentLoggerFactory());
        }

        private static Animal LinkedAnimal(int categoryId, string name, bool active)
        {
            return new Animal
            {
                Name = name,
                Species = "Test species",
                CategoryId = categoryId,
                IsActive = active
            };
        }
    }
}
