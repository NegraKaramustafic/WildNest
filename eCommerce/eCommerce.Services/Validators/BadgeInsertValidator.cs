using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class BadgeInsertValidator : AbstractValidator<BadgeInsertRequest>
    {
        public BadgeInsertValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Description cannot exceed 300 characters.");

            RuleFor(x => x.IconUrl)
                .MaximumLength(500).WithMessage("Icon URL cannot exceed 500 characters.");

            RuleFor(x => x.ImageBase64)
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage)
                .When(x => !string.IsNullOrEmpty(x.ImageBase64));

            RuleFor(x => x.Criteria)
                .IsInEnum().WithMessage("Award rule is required.");
        }
    }
}
