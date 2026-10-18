namespace eCommerce.Services
{
    public static class PasswordResetMessages
    {
        public const string InvalidOrExpired = "This reset code is invalid or has expired.";

        public const string TooManyAttempts = "Too many incorrect attempts. Request a new reset code.";

        public const string ResetSuccess = "Your password has been reset. Sign in with the new password.";
    }
}
