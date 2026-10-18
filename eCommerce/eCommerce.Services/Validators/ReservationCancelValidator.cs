using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ReservationCancelValidator : AbstractValidator<ReservationCancelRequest>
    {
        public const int MaxReasonLength = 500;

        public ReservationCancelValidator()
        {
            RuleFor(x => x.Reason)
                .Must(reason => !string.IsNullOrWhiteSpace(reason))
                .WithMessage("A reason is required.")
                .MaximumLength(MaxReasonLength)
                .WithMessage("Reason must be at most 500 characters.");
        }
    }
}
