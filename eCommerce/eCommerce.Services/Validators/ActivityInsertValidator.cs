using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ActivityInsertValidator : AbstractValidator<ActivityInsertRequest>
    {
        public ActivityInsertValidator()
        {
            RuleFor(x => x.ImageUrl)
                .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters.");

            RuleFor(x => x.ImageBase64)
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage)
                .When(x => !string.IsNullOrEmpty(x.ImageBase64));
        }
    }
}
