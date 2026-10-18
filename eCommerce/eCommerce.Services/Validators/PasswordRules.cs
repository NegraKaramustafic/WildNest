using FluentValidation;

namespace eCommerce.Services.Validators
{
    /// <summary>
    /// Password rules shared by registration and password reset.
    /// </summary>
    public static class PasswordRules
    {
        public static void Apply<T>(IRuleBuilderInitial<T, string> rule)
        {
            rule.NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");
        }
    }
}
