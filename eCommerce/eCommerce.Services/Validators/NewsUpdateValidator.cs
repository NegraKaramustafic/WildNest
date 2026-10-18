using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class NewsUpdateValidator : AbstractValidator<NewsUpdateRequest>
    {
        public NewsUpdateValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Text is required.")
                .MaximumLength(4000).WithMessage("Text cannot exceed 4000 characters.");

            RuleFor(x => x.ImageBase64)
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage)
                .When(x => !string.IsNullOrEmpty(x.ImageBase64));

            RuleFor(x => x.PublishedAt)
                .Must(date => date != default)
                .WithMessage("Date and time are required.");

            RuleFor(x => x.PublishedAt)
                .Must(date => date == default || (date.Year >= 2000 && date.Year <= 2100))
                .WithMessage("Date and time must be between 01 Jan 2000 and 31 Dec 2100.");
        }
    }
}
