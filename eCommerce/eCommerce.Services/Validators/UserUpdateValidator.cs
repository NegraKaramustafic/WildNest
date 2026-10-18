using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class UserUpdateValidator : AbstractValidator<UserUpdateRequest>
    {
        public UserUpdateValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(50).WithMessage("First name cannot exceed 50 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email must be a valid email address.")
                .MaximumLength(100).WithMessage("Email cannot exceed 100 characters.");

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(100).WithMessage("Username cannot exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                .Matches(@"^[0-9+\-() ]{6,20}$").WithMessage("Phone number must be 6 to 20 characters and may contain only digits, spaces, and + - ( ).")
                .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

            RuleFor(x => x.ProfileImageBase64)
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage)
                .When(x => !string.IsNullOrEmpty(x.ProfileImageBase64));

            RuleFor(x => x.Role)
                .Must(RoleNames.IsKnown)
                .WithMessage("Role must be Admin or Customer.")
                .When(x => !string.IsNullOrWhiteSpace(x.Role));

            When(x => !string.IsNullOrWhiteSpace(x.Password), () =>
            {
                RuleFor(x => x.Password!)
                    .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
                    .MaximumLength(100).WithMessage("Password cannot exceed 100 characters.");

                RuleFor(x => x.ConfirmPassword)
                    .Equal(x => x.Password).WithMessage("Password confirmation does not match the new password.");
            });

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Enter a new password, or leave both password fields blank.")
                .When(x => string.IsNullOrWhiteSpace(x.Password) && !string.IsNullOrWhiteSpace(x.ConfirmPassword));
        }
    }
}
