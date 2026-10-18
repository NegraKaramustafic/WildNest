using eCommerce.Model.Requests;
using eCommerce.Services.Images;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class NewsInsertValidator : AbstractValidator<NewsInsertRequest>
    {
        public NewsInsertValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(150).WithMessage("Title cannot exceed 150 characters.");

            RuleFor(x => x.Text)
                .NotEmpty().WithMessage("Text is required.")
                .MaximumLength(4000).WithMessage("Text cannot exceed 4000 characters.");

            RuleFor(x => x.ImageBase64)
                .NotEmpty().WithMessage("Image is required. Use a PNG or JPEG file of at most 512 KB.")
                .MaximumLength(ImageContent.MaxBase64Length).WithMessage(ImageContent.InvalidMessage);

            RuleFor(x => x.PublishedAt)
                .Must(date => date != default)
                .WithMessage("Date and time are required.");

            RuleFor(x => x.PublishedAt)
                .Must(date => date == default || (date.Year >= 2000 && date.Year <= 2100))
                .WithMessage("Date and time must be between 01 Jan 2000 and 31 Dec 2100.");
        }
    }
}
