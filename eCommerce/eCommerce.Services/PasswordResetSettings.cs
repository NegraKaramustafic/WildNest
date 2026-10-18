using eCommerce.Common.Services;

namespace eCommerce.Services
{
    public sealed class PasswordResetSettings
    {
        public int LifetimeMinutes { get; init; } = 15;

        public int CooldownSeconds { get; init; } = 60;

        public int MaxAttempts { get; init; } = 5;

        public static PasswordResetSettings FromEnvironment()
        {
            return new PasswordResetSettings
            {
                LifetimeMinutes = ReadPositive("PASSWORD_RESET_CODE_LIFETIME_MINUTES", 15),
                CooldownSeconds = ReadPositive("PASSWORD_RESET_COOLDOWN_SECONDS", 60),
                MaxAttempts = ReadPositive("PASSWORD_RESET_MAX_ATTEMPTS", 5)
            };
        }

        private static int ReadPositive(string name, int fallback)
        {
            var text = EnvVariables.Optional(name);
            if (text == null)
            {
                return fallback;
            }

            if (!int.TryParse(text, out var value) || value <= 0)
            {
                throw new InvalidOperationException($"{name} must be a positive integer.");
            }

            return value;
        }
    }
}
