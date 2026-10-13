using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ScheduleInsertValidator : AbstractValidator<ScheduleInsertRequest>
    {
        public ScheduleInsertValidator(ECommerceDbContext dbContext)
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("Aktivnost je obavezna.");

            RuleFor(x => x.Date)
                .Must(date => date != default && date >= DateOnly.FromDateTime(DateTime.Today))
                .WithMessage("Datum ne može biti u prošlosti.");

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
                        nameof(ScheduleInsertRequest.CapacityLeft),
                        $"Preostali kapacitet ne može biti veći od kapaciteta aktivnosti ({activity.Capacity}).");
                }
            });

            RuleFor(x => x).Custom((request, context) =>
            {
                if (request.Status != ScheduleStatus.Active || request.EndTime <= request.StartTime)
                {
                    return;
                }

                var overlaps = dbContext.Schedules.Any(schedule =>
                    schedule.ActivityId == request.ActivityId &&
                    schedule.Date == request.Date &&
                    schedule.Status == ScheduleStatus.Active &&
                    schedule.StartTime < request.EndTime &&
                    request.StartTime < schedule.EndTime);

                if (overlaps)
                {
                    context.AddFailure("Već postoji aktivan termin koji se preklapa za istu aktivnost na isti datum.");
                }
            });
        }
    }
}
