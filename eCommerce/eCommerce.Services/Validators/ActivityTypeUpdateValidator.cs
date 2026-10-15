using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ActivityTypeUpdateValidator : AbstractValidator<ActivityTypeUpdateRequest>
    {
        public ActivityTypeUpdateValidator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Name is required.")
                .Must(name => name.Trim().Length >= 2).WithMessage("Name must be at least 2 characters.")
                .Must(name => name.Trim().Length <= 50).WithMessage("Name cannot exceed 50 characters.");

            RuleFor(x => x.Description)
                .Must(description => string.IsNullOrWhiteSpace(description) || description.Trim().Length <= 500)
                .WithMessage("Description cannot exceed 500 characters.");
        }
    }
}
