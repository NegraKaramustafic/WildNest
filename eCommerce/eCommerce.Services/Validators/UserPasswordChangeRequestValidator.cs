using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class UserPasswordChangeRequestValidator : AbstractValidator<UserPasswordChangeRequest>
    {
        public UserPasswordChangeRequestValidator()
        {
            PasswordRules.Apply(RuleFor(x => x.NewPassword));

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Password confirmation is required.")
                .Equal(x => x.NewPassword).WithMessage("Password confirmation doesn't match the new password.");
        }
    }
}
