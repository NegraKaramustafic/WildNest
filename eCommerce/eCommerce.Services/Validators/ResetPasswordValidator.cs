using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
    {
        public ResetPasswordValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Reset code is required.")
                .Matches(@"^\d{6}$").WithMessage("Reset code must be 6 digits.");

            PasswordRules.Apply(RuleFor(x => x.NewPassword));

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.NewPassword).WithMessage("Password confirmation doesn't match the new password.");
        }
    }
}
