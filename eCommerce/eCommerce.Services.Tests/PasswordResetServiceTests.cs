using System.Text.RegularExpressions;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using eCommerce.Services.Validators;
using eCommerce.Model.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace eCommerce.Services.Tests
{
    public class PasswordResetServiceTests
    {
        private const string Email = "customer1@gmail.com";

        [Fact]
        public async Task ValidCode_ResetsPassword_RotatesStamp_AndDoesNotStoreTheCode()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var cache = new FakeSecurityStampCache();
            var service = CreateService(db, publisher, cache);
            var before = await db.Context.Users.AsNoTracking().SingleAsync(user => user.Email == Email);

            var response = await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            Assert.Equal(ForgotPasswordResponse.GenericMessage, response.Message);

            var message = Assert.Single(publisher.Messages);
            var code = CodeFrom(message);
            Assert.DoesNotContain(code, message.Message);
            Assert.DoesNotContain(code, message.Title);

            var stored = await db.Context.PasswordResetCodes.SingleAsync();
            Assert.NotEqual(code, stored.CodeHash);
            Assert.NotEqual(code, stored.CodeSalt);
            Assert.DoesNotContain(code, stored.CodeHash);
            Assert.True(new CryptoService().Verify(stored.CodeHash, stored.CodeSalt, code));

            await service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            });

            var after = await db.Context.Users.AsNoTracking().SingleAsync(user => user.Email == Email);
            Assert.NotEqual(before.PasswordHash, after.PasswordHash);
            Assert.NotEqual(before.SecurityStamp, after.SecurityStamp);
            Assert.True(new CryptoService().Verify(after.PasswordHash, after.PasswordSalt, "NewPass1"));
            Assert.False(new CryptoService().Verify(after.PasswordHash, after.PasswordSalt, "Test123"));
            Assert.Equal(1, cache.Invalidations);
            Assert.NotNull(await db.Context.PasswordResetCodes.Select(row => row.UsedAt).SingleAsync());
            Assert.Empty(await db.Context.RefreshTokens.Where(token => token.UserId == after.Id).ToListAsync());
        }

        [Fact]
        public async Task UnknownEmail_ReturnsTheSameMessage_AndSendsNothing()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());

            var response = await service.RequestAsync(new ForgotPasswordRequest { Email = "missing@example.com" });

            Assert.Equal(ForgotPasswordResponse.GenericMessage, response.Message);
            Assert.Empty(publisher.Messages);
            Assert.Empty(await db.Context.PasswordResetCodes.ToListAsync());
        }

        [Fact]
        public async Task WrongCode_IsRejected_AndDoesNotChangeThePassword()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);
            var before = await db.Context.Users.AsNoTracking().SingleAsync(user => user.Email == Email);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code == "000000" ? "111111" : "000000",
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            }));

            Assert.Equal(PasswordResetMessages.InvalidOrExpired, error.Message);
            var after = await db.Context.Users.AsNoTracking().SingleAsync(user => user.Email == Email);
            Assert.Equal(before.PasswordHash, after.PasswordHash);
            Assert.Equal(1, await db.Context.PasswordResetCodes.Select(row => row.FailedAttempts).SingleAsync());
        }

        [Fact]
        public async Task ExpiredCode_IsRejected()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);
            var row = await db.Context.PasswordResetCodes.SingleAsync();
            row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.Context.SaveChangesAsync();

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            }));

            Assert.Equal(PasswordResetMessages.InvalidOrExpired, error.Message);
        }

        [Fact]
        public async Task ReusedCode_IsRejected()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);
            var request = new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            };
            await service.ResetAsync(request);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "OtherPass1",
                ConfirmNewPassword = "OtherPass1"
            }));

            Assert.Equal(PasswordResetMessages.InvalidOrExpired, error.Message);
            var user = await db.Context.Users.AsNoTracking().SingleAsync(row => row.Email == Email);
            Assert.True(new CryptoService().Verify(user.PasswordHash, user.PasswordSalt, "NewPass1"));
        }

        [Fact]
        public async Task TooManyAttempts_InvalidatesTheCode()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache(), maxAttempts: 5);
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);
            var wrong = code == "000000" ? "111111" : "000000";

            for (var attempt = 1; attempt <= 4; attempt++)
            {
                var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
                {
                    Email = Email,
                    Code = wrong,
                    NewPassword = "NewPass1",
                    ConfirmNewPassword = "NewPass1"
                }));
                Assert.Equal(PasswordResetMessages.InvalidOrExpired, error.Message);
            }

            var locked = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = wrong,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            }));
            Assert.Equal(PasswordResetMessages.TooManyAttempts, locked.Message);

            var afterLimit = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            }));
            Assert.Equal(PasswordResetMessages.InvalidOrExpired, afterLimit.Message);
            Assert.NotNull(await db.Context.PasswordResetCodes.Select(row => row.InvalidatedAt).SingleAsync());
        }

        [Fact]
        public async Task Cooldown_BlocksASecondCode_UntilTheWindowPasses()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache(), cooldownSeconds: 60);
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var first = CodeFrom(publisher.Messages[0]);

            var again = await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            Assert.Equal(ForgotPasswordResponse.GenericMessage, again.Message);
            Assert.Single(publisher.Messages);
            Assert.Equal(1, await db.Context.PasswordResetCodes.CountAsync());

            var row = await db.Context.PasswordResetCodes.SingleAsync();
            row.CreatedAt = DateTime.UtcNow.AddSeconds(-61);
            await db.Context.SaveChangesAsync();

            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            Assert.Equal(2, publisher.Messages.Count);
            var second = CodeFrom(publisher.Messages[1]);
            Assert.NotEqual(first, second);

            var stale = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = first,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            }));
            Assert.Equal(PasswordResetMessages.InvalidOrExpired, stale.Message);

            await service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = second,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "NewPass1"
            });
        }

        [Fact]
        public async Task WeakPassword_IsRejected()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "short",
                ConfirmNewPassword = "short"
            }));

            Assert.Equal("Password must be at least 6 characters.", error.Message);
        }

        [Fact]
        public async Task MismatchedConfirmation_IsRejected()
        {
            using var db = new SqliteDb();
            var publisher = new CapturingPublisher();
            var service = CreateService(db, publisher, new FakeSecurityStampCache());
            await service.RequestAsync(new ForgotPasswordRequest { Email = Email });
            var code = CodeFrom(publisher.Messages[0]);

            var error = await Assert.ThrowsAsync<ClientException>(() => service.ResetAsync(new ResetPasswordRequest
            {
                Email = Email,
                Code = code,
                NewPassword = "NewPass1",
                ConfirmNewPassword = "OtherPass1"
            }));

            Assert.Equal("Password confirmation doesn't match the new password.", error.Message);
            var user = await db.Context.Users.AsNoTracking().SingleAsync(row => row.Email == Email);
            Assert.True(new CryptoService().Verify(user.PasswordHash, user.PasswordSalt, "Test123"));
        }

        private static PasswordResetService CreateService(
            SqliteDb db,
            CapturingPublisher publisher,
            FakeSecurityStampCache cache,
            int cooldownSeconds = 60,
            int maxAttempts = 5)
        {
            return new PasswordResetService(
                db.Context,
                new CryptoService(),
                publisher,
                cache,
                new ForgotPasswordValidator(),
                new ResetPasswordValidator(),
                new PasswordResetSettings
                {
                    LifetimeMinutes = 15,
                    CooldownSeconds = cooldownSeconds,
                    MaxAttempts = maxAttempts
                },
                new ZooClock("UTC"));
        }

        private static string CodeFrom(NotificationMessage message)
        {
            var match = Regex.Match(message.EmailBody ?? string.Empty, @"\b(\d{6})\b");
            Assert.True(match.Success, "The email body should contain the reset code.");
            return match.Groups[1].Value;
        }
    }

    internal sealed class CapturingPublisher : INotificationPublisher
    {
        public List<NotificationMessage> Messages { get; } = new();

        public Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeSecurityStampCache : ISecurityStampCache
    {
        public int Invalidations { get; private set; }

        public void Invalidate(int userId) => Invalidations++;

        public Task<string?> GetOrLoadAsync(int userId, Func<Task<string?>> load) => load();
    }
}
