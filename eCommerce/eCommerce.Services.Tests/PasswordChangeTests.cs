using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Requests;
using eCommerce.Services.Validators;
using FluentValidation;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class PasswordChangeTests
    {
        [Fact]
        public async Task ShortPassword_IsRejected()
        {
            using var db = new SqliteDb();
            var service = await Prepare(db);
            var before = await HashOf(db);

            var error = await Assert.ThrowsAsync<ValidationException>(() => service.ChangePasswordAsync(new UserPasswordChangeRequest
            {
                Id = 4,
                Password = "OldPass1",
                NewPassword = "1",
                ConfirmNewPassword = "1"
            }));

            Assert.Equal("Password must be at least 6 characters.", error.Errors.Single().ErrorMessage);
            Assert.Equal(before, await HashOf(db));
        }

        [Fact]
        public async Task MissingNewPassword_IsRejected_AndHashStays()
        {
            using var db = new SqliteDb();
            var service = await Prepare(db);
            var before = await HashOf(db);

            var error = await Assert.ThrowsAsync<ValidationException>(() => service.ChangePasswordAsync(new UserPasswordChangeRequest
            {
                Id = 4,
                Password = "OldPass1",
                NewPassword = null!,
                ConfirmNewPassword = "NewPass1"
            }));

            Assert.Equal("Password is required.", error.Errors.First().ErrorMessage);
            db.Context.ChangeTracker.Clear();
            Assert.Equal(before, await HashOf(db));
        }

        [Fact]
        public async Task ValidChange_UpdatesTheHash()
        {
            using var db = new SqliteDb();
            var service = await Prepare(db);
            var before = await HashOf(db);

            await service.ChangePasswordAsync(new UserPasswordChangeRequest
            {
                Id = 4,
                Password = "OldPass1",
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            });

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().SingleAsync(user => user.Id == 4);
            Assert.NotEqual(before, stored.PasswordHash);
            Assert.Equal("salt:NewPass1", stored.PasswordHash);
        }

        private static async Task<UserService> Prepare(SqliteDb db)
        {
            var user = await db.Context.Users.SingleAsync(row => row.Id == 4);
            user.PasswordSalt = "salt";
            user.PasswordHash = "salt:OldPass1";
            await db.Context.SaveChangesAsync();
            db.Context.ChangeTracker.Clear();

            return new UserService(
                db.Context,
                new Mapper(),
                new UserInsertValidator(),
                new UserUpdateValidator(),
                new UserPasswordChangeRequestValidator(),
                new ChangeCrypto(),
                new TestUserAccessor { UserId = 4 },
                new FakeSecurityStampCache(),
                new SilentLoggerFactory());
        }

        private static async Task<string> HashOf(SqliteDb db)
        {
            return await db.Context.Users.AsNoTracking()
                .Where(user => user.Id == 4)
                .Select(user => user.PasswordHash)
                .SingleAsync();
        }

        private sealed class ChangeCrypto : ICryptoService
        {
            public string GenerateHash(string password, string salt) => $"{salt}:{password}";

            public string GenerateSlat() => "salt";

            public bool Verify(string hash, string salt, string password) => hash == $"{salt}:{password}";
        }
    }
}
