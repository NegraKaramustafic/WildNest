using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ScheduleUpdateValidator : AbstractValidator<ScheduleUpdateRequest>
    {
        public ScheduleUpdateValidator()
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("ActivityId is required and must be greater than 0.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime.");

            RuleFor(x => x.CapacityLeft)
                .GreaterThanOrEqualTo(0).When(x => x.CapacityLeft.HasValue)
                .WithMessage("CapacityLeft cannot be negative.");
        }
    }
}
