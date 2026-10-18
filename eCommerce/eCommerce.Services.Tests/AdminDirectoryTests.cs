using System.Reflection;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services.Database;
using eCommerce.Services.Images;
using eCommerce.Services.Recommendation;
using eCommerce.Services.Sentiment;
using eCommerce.Services.Validators;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class AdminDirectoryTests : IDisposable
    {
        private readonly string _uploads = Path.Combine(Path.GetTempPath(), "wildnest-tests", Guid.NewGuid().ToString("N"));

        public AdminDirectoryTests()
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
        public async Task Block_OtherUser_RotatesStamp_RemovesRefreshTokens_AndRejectsSignIn()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            db.Context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 4,
                Token = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await db.Context.SaveChangesAsync();

            var blocked = await service.SetActiveAsync(4, false);

            Assert.False(blocked.IsActive);
            Assert.Contains(4, cache.Invalidated);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.NotEqual(before.SecurityStamp, stored.SecurityStamp);
            Assert.False(await db.Context.RefreshTokens.AnyAsync(token => token.UserId == 4));

            var error = Assert.Throws<ClientException>(() => service.EnsureActiveForSignIn(stored.IsActive));
            Assert.Equal("This account is blocked. Sign-in is not allowed.", error.Message);
        }

        [Fact]
        public async Task Block_Self_IsRejected_AndStampStays()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: true, userId: 1);
            var before = (await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 1)).SecurityStamp;

            var error = await Assert.ThrowsAsync<ClientException>(() => service.SetActiveAsync(1, false));

            Assert.Equal("You cannot block your own account.", error.Message);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 1);
            Assert.True(stored.IsActive);
            Assert.Equal(before, stored.SecurityStamp);
        }

        [Fact]
        public async Task Update_WithoutPassword_KeepsHash_AndWithPassword_RotatesIt()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);

            var unchanged = await service.UpdateAsync(4, Profile(before, password: null, confirm: null));
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
            Assert.Equal(before.SecurityStamp, stored.SecurityStamp);
            Assert.Equal("Dave", unchanged.FirstName);

            await service.UpdateAsync(4, Profile(stored, password: "Secret1", confirm: "Secret1"));
            db.Context.ChangeTracker.Clear();
            var changed = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.NotEqual(before.PasswordHash, changed.PasswordHash);
            Assert.NotEqual(before.SecurityStamp, changed.SecurityStamp);
            Assert.Contains(4, cache.Invalidated);
        }

        [Fact]
        public async Task Update_MismatchedPassword_DoesNotChangeTheHash()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);

            await Assert.ThrowsAsync<ValidationException>(() =>
                service.UpdateAsync(4, Profile(before, password: "Secret1", confirm: "Secret2")));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
        }

        [Fact]
        public async Task Update_SelfService_IgnoresPassword_AndKeepsTheStamp()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: false, userId: 4);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);

            await service.UpdateAsync(4, Profile(before, password: "Secret1", confirm: "Secret1"));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
            Assert.Equal(before.PasswordSalt, stored.PasswordSalt);
            Assert.Equal(before.SecurityStamp, stored.SecurityStamp);
            Assert.Empty(cache.Invalidated);
        }

        [Fact]
        public async Task Update_OwnAccountAsAdministrator_IgnoresPassword()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 1);

            await service.UpdateAsync(1, Profile(before, password: "Secret1", confirm: "Secret1"));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 1);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
            Assert.Equal(before.SecurityStamp, stored.SecurityStamp);
        }

        [Fact]
        public async Task Update_RoleChange_RotatesStamp_RemovesRefreshTokens_AndInvalidatesCache()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            db.Context.RefreshTokens.Add(new RefreshToken
            {
                UserId = 4,
                Token = "refresh-token",
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await db.Context.SaveChangesAsync();

            var updated = await service.UpdateAsync(4, Profile(before, password: null, confirm: null, role: Roles.Admin));

            Assert.Equal(Roles.Admin, updated.Role);
            Assert.Contains(4, cache.Invalidated);
            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.NotEqual(before.SecurityStamp, stored.SecurityStamp);
            Assert.Equal(before.PasswordHash, stored.PasswordHash);
            Assert.False(await db.Context.RefreshTokens.AnyAsync(token => token.UserId == 4));
        }

        [Fact]
        public async Task Update_SameRole_DoesNotRotateTheStamp()
        {
            using var db = new SqliteDb();
            var cache = new RecordingStampCache();
            var service = Users(db.Context, cache, admin: true, userId: 1);
            var before = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);

            await service.UpdateAsync(4, Profile(before, password: null, confirm: null, role: Roles.Customer));

            db.Context.ChangeTracker.Clear();
            var stored = await db.Context.Users.AsNoTracking().FirstAsync(user => user.Id == 4);
            Assert.Equal(before.SecurityStamp, stored.SecurityStamp);
            Assert.Empty(cache.Invalidated);
        }

        [Fact]
        public async Task Insert_IgnoresRoleUnlessTheCallerIsAnAdministrator()
        {
            using var db = new SqliteDb();
            var customerCaller = Users(db.Context, new RecordingStampCache(), admin: false, userId: null);
            var created = await customerCaller.InsertAsync(NewUser("pat", Roles.Admin));

            Assert.Equal(Roles.Customer, created.Role);
            var roleId = await db.Context.UserRoles.Where(link => link.UserId == created.Id).Select(link => link.RoleId).SingleAsync();
            Assert.Equal(2, roleId);

            var adminCaller = Users(db.Context, new RecordingStampCache(), admin: true, userId: 1);
            var admin = await adminCaller.InsertAsync(NewUser("sam", Roles.Admin));
            Assert.Equal(Roles.Admin, admin.Role);
        }

        [Fact]
        public async Task Delete_Self_IsRejected()
        {
            using var db = new SqliteDb();
            var service = Users(db.Context, new RecordingStampCache(), admin: true, userId: 1);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(1));

            Assert.Equal("You cannot delete your own account.", error.Message);
            Assert.True(await db.Context.Users.AnyAsync(user => user.Id == 1));
        }

        [Fact]
        public async Task Badge_DuplicateName_AndDeleteInUse_AreRejected()
        {
            using var db = new SqliteDb();
            var service = Badges(db.Context);

            var duplicate = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new BadgeInsertRequest
            {
                Name = " wildlife guardian ",
                Criteria = BadgeCriteria.FirstAdoption
            }));
            Assert.Contains("already exists.", duplicate.Errors.Single().ErrorMessage);

            db.Context.UserBadges.Add(new UserBadge { UserId = 4, BadgeId = 1, EarnedAt = DateTime.UtcNow });
            await db.Context.SaveChangesAsync();
            var inUse = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(1));
            Assert.Equal("This badge has been awarded to users and cannot be deleted.", inUse.Message);

            var created = await service.InsertAsync(new BadgeInsertRequest
            {
                Name = "Night Keeper",
                Criteria = BadgeCriteria.FirstBooking
            });
            await service.DeleteAsync(created.Id);
            Assert.False(await db.Context.Badges.AnyAsync(badge => badge.Id == created.Id));
        }

        [Fact]
        public async Task Badge_RejectsAnImageThatIsNotPngOrJpeg()
        {
            using var db = new SqliteDb();
            var service = Badges(db.Context);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.InsertAsync(new BadgeInsertRequest
            {
                Name = "Broken image",
                Criteria = BadgeCriteria.FirstBooking,
                ImageBase64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
                ImageContentType = "image/gif"
            }));

            Assert.Equal(ImageContent.InvalidMessage, error.Message);
            Assert.False(Directory.EnumerateFiles(_uploads).Any());
        }

        [Fact]
        public async Task ZooPartner_DeleteIsBlockedWhenDonationsExist_AndUpdateKeepsRaisedAmount()
        {
            using var db = new SqliteDb();
            var service = Partners(db.Context);
            db.Context.Donations.Add(new Donation
            {
                UserId = 4,
                ZooPartnerId = 1,
                Amount = 25,
                CreatedAt = DateTime.UtcNow
            });
            await db.Context.SaveChangesAsync();

            var blocked = await Assert.ThrowsAsync<ClientException>(() => service.DeleteAsync(1));
            Assert.Equal("This zoo partner has donations and cannot be deleted. Deactivate it instead.", blocked.Message);

            var updated = await service.UpdateAsync(1, new ZooPartnerUpdateRequest
            {
                Name = "Sunbird Sanctuary",
                Location = "Costa Rica",
                GoalAmount = 21000,
                IsActive = false
            });
            Assert.Equal(12400m, updated.CurrentAmount);
            Assert.False(updated.IsActive);
            Assert.Equal(1, updated.DonationCount);

            var duplicate = await Assert.ThrowsAsync<ValidationException>(() => service.InsertAsync(new ZooPartnerInsertRequest
            {
                Name = "sunbird sanctuary",
                Location = "Elsewhere",
                GoalAmount = 10
            }));
            Assert.Contains("already exists.", duplicate.Errors.Single().ErrorMessage);
        }

        [Fact]
        public async Task ReviewDelete_RecalculatesSentimentStatistics_AndMarksRecommendationsDirty()
        {
            using var db = new SqliteDb();
            var cache = new RecommendationModelCache();
            var admin = Reviews(db.Context, cache, admin: true, userId: 1);
            var before = await admin.GetSentimentStatisticsAsync();
            var review = await db.Context.Reviews.AsNoTracking().OrderBy(item => item.Id).FirstAsync();
            SetDirty(cache, false);

            await admin.DeleteAsync(review.Id);

            var after = await admin.GetSentimentStatisticsAsync();
            Assert.Equal(before.TotalReviews - 1, after.TotalReviews);
            Assert.False(await db.Context.Reviews.AnyAsync(item => item.Id == review.Id));
            Assert.True(ReadDirty(cache));

            var customer = Reviews(db.Context, new RecommendationModelCache(), admin: false, userId: 5);
            var other = await db.Context.Reviews.AsNoTracking().FirstAsync(item => item.UserId != 5);
            await Assert.ThrowsAsync<NotFoundException>(() => customer.DeleteAsync(other.Id));
        }

        [Fact]
        public async Task ReviewFilters_RejectAnInvertedDateRange_AndARatingOutsideOneToFive()
        {
            using var db = new SqliteDb();
            var service = Reviews(db.Context, new RecommendationModelCache(), admin: true, userId: 1);

            var dates = await Assert.ThrowsAsync<ClientException>(() => service.GetAllAsync(new ReviewSearchObject
            {
                DateFrom = new DateOnly(2026, 5, 2),
                DateTo = new DateOnly(2026, 5, 1),
                Page = 1,
                PageSize = 10
            }));
            Assert.Equal("From date cannot be after to date.", dates.Message);

            var rating = await Assert.ThrowsAsync<ClientException>(() => service.GetAllAsync(new ReviewSearchObject
            {
                Rating = 9,
                Page = 1,
                PageSize = 10
            }));
            Assert.Equal("Rating must be between 1 and 5.", rating.Message);

            var matches = await service.GetAllAsync(new ReviewSearchObject
            {
                Rating = 5,
                Page = 1,
                PageSize = 100,
                IncludeTotalCount = true
            });
            Assert.NotEmpty(matches.Items);
            Assert.All(matches.Items, review => Assert.Equal(5, review.Rating));
        }

        private UserService Users(ECommerceDbContext context, RecordingStampCache cache, bool admin, int? userId)
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

        private BadgeService Badges(ECommerceDbContext context)
        {
            return new BadgeService(
                context,
                new Mapper(),
                new BadgeInsertValidator(),
                new BadgeUpdateValidator(),
                new FileStoredImageService(new UploadSettings(_uploads)),
                new SilentLoggerFactory());
        }

        private ZooPartnerService Partners(ECommerceDbContext context)
        {
            return new ZooPartnerService(
                context,
                new Mapper(),
                new ZooPartnerInsertValidator(),
                new ZooPartnerUpdateValidator(),
                new FileStoredImageService(new UploadSettings(_uploads)),
                new SilentLoggerFactory());
        }

        private static ReviewService Reviews(ECommerceDbContext context, RecommendationModelCache cache, bool admin, int userId)
        {
            return new ReviewService(
                context,
                new Mapper(),
                new ReviewInsertValidator(),
                new ReviewUpdateValidator(),
                new TestUserAccessor { Admin = admin, UserId = userId },
                cache,
                new IdleSentiment(),
                new SilentLoggerFactory());
        }

        private static IMapper UserMapper()
        {
            var config = new TypeAdapterConfig();
            config.NewConfig<User, UserResponse>()
                .AfterMapping((User src, UserResponse dest) =>
                {
                    var role = src.UserRoles
                        .Select(link => link.Role?.Name)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        dest.Role = role;
                    }
                });
            return new Mapper(config);
        }

        private static UserUpdateRequest Profile(User user, string? password, string? confirm, string? role = null)
        {
            return new UserUpdateRequest
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                Role = role,
                Password = password,
                ConfirmPassword = confirm
            };
        }

        private static UserInsertRequest NewUser(string username, string role)
        {
            return new UserInsertRequest
            {
                FirstName = "Pat",
                LastName = "Example",
                Email = $"{username}@example.com",
                Username = username,
                Password = "Secret1",
                IsActive = true,
                Role = role
            };
        }

        private static void SetDirty(RecommendationModelCache cache, bool value)
        {
            typeof(RecommendationModelCache)
                .GetField("_isDirty", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(cache, value);
        }

        private static bool ReadDirty(RecommendationModelCache cache)
        {
            return (bool)typeof(RecommendationModelCache)
                .GetField("_isDirty", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(cache)!;
        }

        private sealed class RecordingStampCache : ISecurityStampCache
        {
            public List<int> Invalidated { get; } = new();

            public Task<string?> GetOrLoadAsync(int userId, Func<Task<string?>> load) => load();

            public void Invalidate(int userId) => Invalidated.Add(userId);
        }

        private sealed class FakeCrypto : ICryptoService
        {
            public string GenerateHash(string password, string salt) => $"{salt}:{password}";

            public string GenerateSlat() => "salt";

            public bool Verify(string hash, string salt, string password) => hash == $"{salt}:{password}";
        }

        private sealed class IdleSentiment : ISentimentService
        {
            public bool IsReady => true;

            public SentimentTrainingReport? LastReport => null;

            public SentimentClassification Classify(string? text) => SentimentClassification.Unlabeled;

            public Task<bool> EnsureReadyAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
        }
    }
}
