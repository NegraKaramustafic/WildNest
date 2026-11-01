using eCommerce.Model;
using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class TicketUpdateValidator : AbstractValidator<TicketUpdateRequest>
    {
        public TicketUpdateValidator(ZooClock zooClock)
        {
            RuleFor(x => x.VisitDate)
                .Must(date => date != default)
                .WithMessage(TicketLimits.VisitDateRequiredMessage);

            RuleFor(x => x.VisitDate)
                .Must(date => date >= zooClock.Today)
                .When(x => x.VisitDate != default)
                .WithMessage(TicketLimits.VisitDatePastMessage);

            RuleFor(x => x.VisitorCount)
                .InclusiveBetween(TicketLimits.MinimumVisitors, TicketLimits.MaximumVisitors)
                .WithMessage(TicketLimits.VisitorCountMessage);

            RuleFor(x => x.PricePerPerson)
                .GreaterThanOrEqualTo(0).WithMessage("PricePerPerson cannot be negative.");
        }
    }
}
