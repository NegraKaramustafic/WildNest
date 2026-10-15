using eCommerce.Model.Enums;
using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Validators
{
    public class ScheduleInsertValidator : AbstractValidator<ScheduleInsertRequest>
    {
        public ScheduleInsertValidator(ECommerceDbContext dbContext, ZooClock zooClock)
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("Activity is required.");

            RuleFor(x => x.Date)
                .Must(date => date != default && date >= zooClock.Today)
                .WithMessage("The date cannot be in the past.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time.");

            RuleFor(x => x.CapacityLeft)
                .GreaterThanOrEqualTo(0).When(x => x.CapacityLeft.HasValue)
                .WithMessage("Remaining capacity cannot be negative.");

            RuleFor(x => x).CustomAsync(async (request, context, cancellation) =>
            {
                if (!request.CapacityLeft.HasValue)
                {
                    return;
                }

                var activity = await dbContext.Activities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == request.ActivityId, cancellation);
                if (activity != null && request.CapacityLeft.Value > activity.Capacity)
                {
                    context.AddFailure(
                        nameof(ScheduleInsertRequest.CapacityLeft),
                        $"Remaining capacity cannot exceed the activity capacity ({activity.Capacity}).");
                }
            });

            RuleFor(x => x).CustomAsync(async (request, context, cancellation) =>
            {
                if (request.Status != ScheduleStatus.Active || request.EndTime <= request.StartTime)
                {
                    return;
                }

                var overlaps = await dbContext.Schedules.AnyAsync(schedule =>
                    schedule.ActivityId == request.ActivityId &&
                    schedule.Date == request.Date &&
                    schedule.Status == ScheduleStatus.Active &&
                    schedule.StartTime < request.EndTime &&
                    request.StartTime < schedule.EndTime, cancellation);

                if (overlaps)
                {
                    context.AddFailure("An active schedule already overlaps for the same activity on the same date.");
                }
            });
        }
    }
}
