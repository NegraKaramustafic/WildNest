using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class DonationReportValidator : AbstractValidator<DonationReportRequest>
    {
        public DonationReportValidator()
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

            RuleFor(x => x.ZooPartnerId)
                .GreaterThan(0)
                .When(x => x.ZooPartnerId.HasValue)
                .WithMessage("Select a valid zoo partner.");
        }

        private static bool IsReasonableDate(DateOnly date) => date.Year is >= 2000 and <= 2100;
    }
}
