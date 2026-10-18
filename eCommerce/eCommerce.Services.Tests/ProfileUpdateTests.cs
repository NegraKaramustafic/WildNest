using eCommerce.Common.Services.CryptoService;
using eCommerce.Model;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Validators;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class ProfileUpdateTests
    {
        [Fact]
        public async Task Update_OwnProfile_ChangesPersonalDataAndImage_AndIgnoresRoleActiveFlagAndPassword()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: false, userId: 4);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            db.Context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 4,
                Token = "session",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await db.Context.SaveChangesAsync();
            var photo = JpegBytes(32);

            var updated = await service.UpdateAsync(4, new UserUpdateRequest
            {
                FirstName = "Dana",
                LastName = "Nest",
                Email = "dana.nest@gmail.com",
                Username = "dananest",
                PhoneNumber = "+38761111222",
                IsActive = false,
                Role = Roles.Admin,
                Password = "Secret1",
                ConfirmPassword = "Secret1",
                ProfileImageBase64 = Convert.ToBase64String(photo)
            });

            Assert.Equal("Dana", updated.FirstName);
            Assert.Equal("Nest", updated.LastName);
            Assert.Equal("dana.nest@gmail.com", updated.Email);
            Assert.Equal("dananest", updated.Username);
            Assert.Equal("+38761111222", updated.PhoneNumber);
            Assert.Equal(Convert.ToBase64String(photo), updated.ProfileImageBase64);
            Assert.True(updated.IsActive);
            Assert.Equal(Roles.Customer, updated.Role);

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
            Assert.Equal(before.PasswordSalt, stored.PasswordSalt);
            Assert.Equal(before.SecurityStamp, stored.SecurityStamp);
            Assert.True(stored.IsActive);
            Assert.Equal(Roles.Customer, await RoleName(db.Context, 4));
            Assert.True(await db.Context.RefreshTokens.AnyAsync(token => token.UserId == 4 && token.Token == "session"));
            Assert.Empty(cache.Invalidated);
        }

        [Fact]
        public async Task Update_AnotherUser_IsRejected_AndNothingChanges()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: false, userId: 4);
            var other = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 5);

            var error = await Assert.ThrowsAsync<NotFoundException>(() =>
                service.UpdateAsync(5, Profile(other)));

            Assert.Equal("User not found.", error.Message);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 5);
            Assert.Equal(other.FirstName, stored.FirstName);
            Assert.Equal(other.Email, stored.Email);
            Assert.Equal(other.PasswordHash, stored.PasswordHash);
            Assert.Equal(Roles.Customer, await RoleName(db.Context, 5));
        }

        [Fact]
        public async Task Update_DuplicateEmailAndUsername_AreRejectedWithEnglishMessages()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: false, userId: 4);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            var other = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 5);

            var email = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(4, Profile(before, email: other.Email)));
            Assert.Equal($"Email '{other.Email}' is already in use.", email.Message);

            var username = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(4, Profile(before, username: other.Username)));
            Assert.Equal($"Username '{other.Username}' is already in use.", username.Message);

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.Email, stored.Email);
            Assert.Equal(before.Username, stored.Username);
        }

        [Fact]
        public async Task Update_ProfileImage_RejectsTypeAndSize_AndClearsWhenEmpty()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: false, userId: 4);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            var photo = Convert.ToBase64String(JpegBytes(24));
            await service.UpdateAsync(4, Profile(before, image: photo));

            var wrongType = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(4, Profile(before, image: Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }))));
            Assert.Equal(ImageContent.InvalidMessage, wrongType.Message);

            var tooLarge = new byte[ImageContent.MaxBytes + 1];
            tooLarge[0] = 0xFF;
            tooLarge[1] = 0xD8;
            tooLarge[2] = 0xFF;
            var size = await Assert.ThrowsAsync<ClientException>(() =>
                service.UpdateAsync(4, Profile(before, image: Convert.ToBase64String(tooLarge))));
            Assert.Equal(ImageContent.InvalidMessage, size.Message);

            db.Context.ChangeTracker.Clear();
            var kept = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(photo, kept.ProfileImageBase64);

            var cleared = await service.UpdateAsync(4, Profile(before, image: ""));
            Assert.Null(cleared.ProfileImageBase64);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Null(stored.ProfileImageBase64);
        }

        [Fact]
        public async Task Update_InvalidPersonalData_DoesNotSave()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: false, userId: 4);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);

            var email = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(4, Profile(before, email: "not-an-email")));
            Assert.Contains("Email must be a valid email address.", email.Errors.Select(error => error.ErrorMessage));

            var phone = await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(4, Profile(before, phone: "call-me")));
            Assert.Contains(
                "Phone number must be 6 to 20 characters and may contain only digits, spaces, and + - ( ).",
                phone.Errors.Select(error => error.ErrorMessage));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.Email, stored.Email);
            Assert.Equal(before.PhoneNumber, stored.PhoneNumber);
        }

        private static UserService Users(ECommerceDbContext context, RecordingStampCache cache, bool admin, int? userId)
        {
            return new UserService(
                context,
                UserMapper(),
                new UserInsertValidator(),
                new UserUpdateValidator(),
                new FakeCrypto(),
                new TestUserAccessor { Admin = admin, UserId = userId },
                cache,
                new SilentLoggerFactory());
        }

        private static IMapper UserMapper()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<UserUpdateRequest, User>()
                .Ignore(dest => dest.PasswordHash)
                .Ignore(dest => dest.PasswordSalt)
                .Ignore(dest => dest.SecurityStamp)
                .IgnoreNullValues(true);
            config.NewConfig<User, UserResponse>()
                .Ignore(dest => dest.Role)
                .AfterMapping((User src, UserResponse dest) =>
                {
                    var role = src.UserRoles
                        .Select(link => link.Role?.Name)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        dest.Role = role;
                    }
                })
                .IgnoreNullValues(true);
            return new Mapper(config);
        }

        private static UserUpdateRequest Profile(
            User user,
            string? email = null,
            string? username = null,
            string? phone = null,
            string? image = null)
        {
            return new UserUpdateRequest
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = email ?? user.Email,
                Username = username ?? user.Username,
                PhoneNumber = phone ?? user.PhoneNumber,
                IsActive = user.IsActive,
                ProfileImageBase64 = image
            };
        }

        private static async Task<string> RoleName(ECommerceDbContext context, int userId)
        {
            return await context.UserRoles
                .Where(link => link.UserId == userId)
                .Select(link => link.Role.Name)
                .SingleAsync();
        }

        private static byte[] JpegBytes(int extra)
        {
            var bytes = new byte[3 + extra];
            bytes[0] = 0xFF;
            bytes[1] = 0xD8;
            bytes[2] = 0xFF;
            return bytes;
        }

        private sealed class FakeCrypto : ICryptoService
        {
            public string GenerateHash(string password, string salt) => $"{salt}:{password}";

            public string GenerateSlat() => "salt";

            public bool Verify(string hash, string salt, string password) => hash == $"{salt}:{password}";
        }

        private sealed class RecordingStampCache : ISecurityStampCache
        {
            public List<int> Invalidated { get; } = new();

            public Task<string?> GetOrLoadAsync(int userId, Func<Task<string?>> load) => load();

            public void Invalidate(int userId) => Invalidated.Add(userId);
        }
    }
}
