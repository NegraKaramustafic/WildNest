using eCommerce.Model;
using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class TicketInsertValidator : AbstractValidator<TicketInsertRequest>
    {
        public TicketInsertValidator(ZooClock zooClock)
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
        }
    }
}
