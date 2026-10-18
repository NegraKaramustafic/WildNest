using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ReservationReportValidator : AbstractValidator<ReservationReportRequest>
    {
        public ReservationReportValidator()
        {
            RuleFor(x => x.DateFrom)
                .Must(date => date != default)
                .WithMessage("From date is required.");

            RuleFor(x => x.DateFrom)
                .Must(IsReasonableDate)
                .When(x => x.DateFrom != default)
                .WithMessage("Enter a from date between 01 Jan 2000 and 31 Dec 2100.");

            RuleFor(x => x.DateTo)
                .Must(date => date != default)
                .WithMessage("To date is required.");

            RuleFor(x => x.DateTo)
                .Must(IsReasonableDate)
                .When(x => x.DateTo != default)
                .WithMessage("Enter a to date between 01 Jan 2000 and 31 Dec 2100.");

            RuleFor(x => x.DateTo)
                .Must((request, dateTo) => request.DateFrom <= dateTo)
                .When(x => IsReasonableDate(x.DateFrom) && IsReasonableDate(x.DateTo))
                .WithMessage("From date cannot be after to date.");

            RuleFor(x => x.ActivityId)
                .GreaterThan(0)
                .When(x => x.ActivityId.HasValue)
                .WithMessage("Select a valid activity.");

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue)
                .WithMessage("Status must be Pending, Confirmed, Completed, or Cancelled.");
        }

        private static bool IsReasonableDate(DateOnly date) => date.Year is >= 2000 and <= 2100;
    }
}
