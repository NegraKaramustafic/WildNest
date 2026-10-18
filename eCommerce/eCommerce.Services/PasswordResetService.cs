using System.Globalization;
using System.Security.Cryptography;
using eCommerce.Common.Services.CryptoService;
using eCommerce.Model.Enums;
using eCommerce.Model.Exceptions;
using eCommerce.Model.Messages;
using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly ICryptoService _cryptoService;
        private readonly INotificationPublisher _publisher;
        private readonly ISecurityStampCache _securityStampCache;
        private readonly IValidator<ForgotPasswordRequest> _forgotValidator;
        private readonly IValidator<ResetPasswordRequest> _resetValidator;
        private readonly PasswordResetSettings _settings;
        private readonly ZooClock _clock;

        public PasswordResetService(
            ECommerceDbContext dbContext,
            ICryptoService cryptoService,
            INotificationPublisher publisher,
            ISecurityStampCache securityStampCache,
            IValidator<ForgotPasswordRequest> forgotValidator,
            IValidator<ResetPasswordRequest> resetValidator,
            PasswordResetSettings settings,
            ZooClock clock)
        {
            _dbContext = dbContext;
            _cryptoService = cryptoService;
            _publisher = publisher;
            _securityStampCache = securityStampCache;
            _forgotValidator = forgotValidator;
            _resetValidator = resetValidator;
            _settings = settings;
            _clock = clock;
        }

        public async Task<ForgotPasswordResponse> RequestAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
        {
            request.Email = (request.Email ?? string.Empty).Trim();
            await ThrowFirstErrorAsync(_forgotValidator, request, cancellationToken);

            var email = request.Email.ToLowerInvariant();
            var userId = await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Email.ToLower() == email && user.IsActive)
                .Select(user => (int?)user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (userId == null)
            {
                return GenericResponse();
            }

            var now = DateTime.UtcNow;
            var code = GenerateCode();
            var salt = _cryptoService.GenerateSlat();
            var hash = _cryptoService.GenerateHash(code, salt);
            var expiresAt = now.AddMinutes(_settings.LifetimeMinutes);
            var issued = false;

            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                var cooldownSince = now.AddSeconds(-_settings.CooldownSeconds);
                var latestCreated = await _dbContext.PasswordResetCodes
                    .Where(row => row.UserId == userId.Value)
                    .OrderByDescending(row => row.CreatedAt)
                    .Select(row => (DateTime?)row.CreatedAt)
                    .FirstOrDefaultAsync(cancellationToken);

                if (latestCreated.HasValue && latestCreated.Value >= cooldownSince)
                {
                    return;
                }

                var active = await _dbContext.PasswordResetCodes
                    .Where(row => row.UserId == userId.Value && row.UsedAt == null && row.InvalidatedAt == null)
                    .ToListAsync(cancellationToken);
                foreach (var row in active)
                {
                    row.InvalidatedAt = now;
                }

                _dbContext.PasswordResetCodes.Add(new PasswordResetCode
                {
                    UserId = userId.Value,
                    CodeHash = hash,
                    CodeSalt = salt,
                    CreatedAt = now,
                    ExpiresAt = expiresAt
                });
                await _dbContext.SaveChangesAsync(cancellationToken);
                issued = true;
            });

            if (!issued)
            {
                return GenericResponse();
            }

            var expiresText = FormatLocal(expiresAt);
            var safeMessage = $"A password reset code was sent to your email. It expires in {_settings.LifetimeMinutes} minutes.";
            var emailBody =
                $"Your WildNest password reset code is {code}.\n\n" +
                $"This code expires on {expiresText}. It can be used once.\n\n" +
                "If you did not request a password reset, you can ignore this email.";

            await _publisher.PublishAsync(new NotificationMessage
            {
                MessageId = Guid.NewGuid(),
                UserId = userId.Value,
                Type = NotificationType.PasswordReset,
                Title = "Password reset code sent",
                Message = safeMessage,
                EmailSubject = "Your WildNest password reset code",
                EmailBody = emailBody,
                RelatedEntityType = "User",
                RelatedEntityId = userId.Value
            }, cancellationToken);

            return GenericResponse();
        }

        public async Task ResetAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            request.Email = (request.Email ?? string.Empty).Trim();
            request.Code = (request.Code ?? string.Empty).Trim();
            await ThrowFirstErrorAsync(_resetValidator, request, cancellationToken);

            var email = request.Email.ToLowerInvariant();
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(row => row.Email.ToLower() == email && row.IsActive, cancellationToken);

            if (user == null)
            {
                throw new ClientException(PasswordResetMessages.InvalidOrExpired);
            }

            var now = DateTime.UtcNow;
            var codeRow = await _dbContext.PasswordResetCodes
                .Where(row => row.UserId == user.Id && row.UsedAt == null && row.InvalidatedAt == null)
                .OrderByDescending(row => row.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (codeRow == null)
            {
                throw new ClientException(PasswordResetMessages.InvalidOrExpired);
            }

            if (codeRow.ExpiresAt <= now)
            {
                codeRow.InvalidatedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new ClientException(PasswordResetMessages.InvalidOrExpired);
            }

            if (codeRow.FailedAttempts >= _settings.MaxAttempts)
            {
                codeRow.InvalidatedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new ClientException(PasswordResetMessages.TooManyAttempts);
            }

            if (!_cryptoService.Verify(codeRow.CodeHash, codeRow.CodeSalt, request.Code))
            {
                codeRow.FailedAttempts++;
                if (codeRow.FailedAttempts >= _settings.MaxAttempts)
                {
                    codeRow.InvalidatedAt = now;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                throw new ClientException(codeRow.InvalidatedAt == null
                    ? PasswordResetMessages.InvalidOrExpired
                    : PasswordResetMessages.TooManyAttempts);
            }

            await DbTransactions.ExecuteAsync(_dbContext, async () =>
            {
                var salt = _cryptoService.GenerateSlat();
                user.PasswordSalt = salt;
                user.PasswordHash = _cryptoService.GenerateHash(request.NewPassword, salt);
                user.SecurityStamp = Guid.NewGuid().ToString("N");

                var tokens = await _dbContext.RefreshTokens
                    .Where(token => token.UserId == user.Id)
                    .ToListAsync(cancellationToken);
                _dbContext.RefreshTokens.RemoveRange(tokens);

                codeRow.UsedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
            });

            _securityStampCache.Invalidate(user.Id);
        }

        private ForgotPasswordResponse GenericResponse()
        {
            return new ForgotPasswordResponse
            {
                Message = ForgotPasswordResponse.GenericMessage,
                CooldownSeconds = _settings.CooldownSeconds
            };
        }

        private string FormatLocal(DateTime utc)
        {
            var local = _clock.ToLocal(utc);
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{local:dd MMMM yyyy} at {local:HH:mm} ({_clock.TimeZoneId})");
        }

        private static string GenerateCode()
        {
            return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
        }

        private static async Task ThrowFirstErrorAsync<T>(IValidator<T> validator, T request, CancellationToken cancellationToken)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                throw new ClientException(result.Errors[0].ErrorMessage);
            }
        }
    }
}
