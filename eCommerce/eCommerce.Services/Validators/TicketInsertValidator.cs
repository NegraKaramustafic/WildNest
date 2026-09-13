using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class TicketInsertValidator : AbstractValidator<TicketInsertRequest>
    {
        public TicketInsertValidator()
        {
            RuleFor(x => x.VisitDate)
                .NotEmpty().WithMessage("VisitDate is required.");

            RuleFor(x => x.VisitorCount)
                .GreaterThan(0).WithMessage("VisitorCount must be greater than 0.");
        }
    }
}
