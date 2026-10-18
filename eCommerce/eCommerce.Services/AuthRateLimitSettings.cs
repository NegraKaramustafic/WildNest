using eCommerce.Common.Services;

namespace eCommerce.Services
{
    /// <summary>
    /// Fixed-window limits for sign-in and password-reset endpoints.
    /// Defaults stay above a normal demo (a few mistyped passwords, one reset).
    /// </summary>
    public sealed class AuthRateLimitSettings
    {
        public int WindowSeconds { get; init; } = 60;

        public int LoginPerIp { get; init; } = 30;

        public int LoginPerAccount { get; init; } = 10;

        public int RefreshPerIp { get; init; } = 60;

        public int RefreshPerAccount { get; init; } = 20;

        public int ForgotPasswordPerIp { get; init; } = 10;

        public int ForgotPasswordPerAccount { get; init; } = 5;

        public int ResetPasswordPerIp { get; init; } = 20;

        public int ResetPasswordPerAccount { get; init; } = 10;

        public (int PerIp, int PerAccount) LimitsFor(AuthRateLimitKind kind)
        {
            return kind switch
            {
                AuthRateLimitKind.Login => (LoginPerIp, LoginPerAccount),
                AuthRateLimitKind.Refresh => (RefreshPerIp, RefreshPerAccount),
                AuthRateLimitKind.ForgotPassword => (ForgotPasswordPerIp, ForgotPasswordPerAccount),
                AuthRateLimitKind.ResetPassword => (ResetPasswordPerIp, ResetPasswordPerAccount),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        public static AuthRateLimitSettings FromEnvironment()
        {
            return new AuthRateLimitSettings
            {
                WindowSeconds = ReadPositive("AUTH_RATE_LIMIT_WINDOW_SECONDS", 60),
                LoginPerIp = ReadPositive("AUTH_RATE_LIMIT_LOGIN_PER_IP", 30),
                LoginPerAccount = ReadPositive("AUTH_RATE_LIMIT_LOGIN_PER_ACCOUNT", 10),
                RefreshPerIp = ReadPositive("AUTH_RATE_LIMIT_REFRESH_PER_IP", 60),
                RefreshPerAccount = ReadPositive("AUTH_RATE_LIMIT_REFRESH_PER_ACCOUNT", 20),
                ForgotPasswordPerIp = ReadPositive("AUTH_RATE_LIMIT_FORGOT_PASSWORD_PER_IP", 10),
                ForgotPasswordPerAccount = ReadPositive("AUTH_RATE_LIMIT_FORGOT_PASSWORD_PER_ACCOUNT", 5),
                ResetPasswordPerIp = ReadPositive("AUTH_RATE_LIMIT_RESET_PASSWORD_PER_IP", 20),
                ResetPasswordPerAccount = ReadPositive("AUTH_RATE_LIMIT_RESET_PASSWORD_PER_ACCOUNT", 10)
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
