using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ScheduleUpdateValidator : AbstractValidator<ScheduleUpdateRequest>
    {
        public ScheduleUpdateValidator(ECommerceDbContext dbContext)
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("Aktivnost je obavezna.");

            RuleFor(x => x.Date)
                .Must(date => date != default)
                .WithMessage("Datum je obavezan.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("Vrijeme završetka mora biti nakon vremena početka.");

            RuleFor(x => x.CapacityLeft)
                .GreaterThanOrEqualTo(0).When(x => x.CapacityLeft.HasValue)
                .WithMessage("Preostali kapacitet ne može biti negativan.");

            RuleFor(x => x).Custom((request, context) =>
            {
                if (!request.CapacityLeft.HasValue)
                {
                    return;
                }

                var activity = dbContext.Activities.Find(request.ActivityId);
                if (activity != null && request.CapacityLeft.Value > activity.Capacity)
                {
                    context.AddFailure(
                        nameof(ScheduleUpdateRequest.CapacityLeft),
                        $"Preostali kapacitet ne može biti veći od kapaciteta aktivnosti ({activity.Capacity}).");
                }
            });
        }
    }
}
