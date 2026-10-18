using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ZooPartnerInsertValidator : AbstractValidator<ZooPartnerInsertRequest>
    {
        public ZooPartnerInsertValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.")
                .MaximumLength(200).WithMessage("Location cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            RuleFor(x => x.ImageUrl)
                .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters.");

            RuleFor(x => x.ImageBase64)
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage)
                .When(x => !string.IsNullOrEmpty(x.ImageBase64));

            RuleFor(x => x.GoalAmount)
                .GreaterThan(0).WithMessage("Goal amount must be greater than 0.");
        }
    }
}
