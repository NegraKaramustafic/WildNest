using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class BadgeUpdateValidator : AbstractValidator<BadgeUpdateRequest>
    {
        public BadgeUpdateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(300).WithMessage("Description cannot exceed 300 characters.");

            RuleFor(x => x.IconUrl)
                .MaximumLength(500).WithMessage("IconUrl cannot exceed 500 characters.");

            RuleFor(x => x.Criteria)
                .IsInEnum().WithMessage("Criteria is required.");
        }
    }
}
