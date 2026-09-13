using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ReviewInsertValidator : AbstractValidator<ReviewInsertRequest>
    {
        public ReviewInsertValidator()
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("ActivityId is required and must be greater than 0.");

            RuleFor(x => x.Rating)
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Comment is required.")
                .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
        }
    }
}
