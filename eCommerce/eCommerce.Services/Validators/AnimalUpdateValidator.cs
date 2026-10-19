using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class AnimalUpdateValidator : AbstractValidator<AnimalUpdateRequest>
    {
        public AnimalUpdateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Species)
                .NotEmpty().WithMessage("Species is required.")
                .MaximumLength(100).WithMessage("Species cannot exceed 100 characters.");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId is required and must be greater than 0.");

            RuleFor(x => x.Habitat)
                .MaximumLength(100).WithMessage("Habitat cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.Weight)
                .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.");

            RuleFor(x => x.AdoptionPrice)
                .GreaterThanOrEqualTo(0).When(x => x.AdoptionPrice.HasValue)
                .WithMessage("AdoptionPrice cannot be negative.");
        }
    }
}
