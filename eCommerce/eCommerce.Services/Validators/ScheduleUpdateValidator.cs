using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Validators
{
    public class ScheduleUpdateValidator : AbstractValidator<ScheduleUpdateRequest>
    {
        public ScheduleUpdateValidator(ECommerceDbContext dbContext, ZooClock zooClock)
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("Activity is required.");

            RuleFor(x => x.Date)
                .Must(date => date != default && date >= zooClock.Today)
                .WithMessage("The date cannot be in the past.");

            RuleFor(x => x.EndTime)
                .GreaterThan(x => x.StartTime)
                .WithMessage("End time must be after start time.");

            RuleFor(x => x.Capacity)
                .GreaterThanOrEqualTo(0).When(x => x.Capacity.HasValue)
                .WithMessage("Capacity cannot be negative.");

            RuleFor(x => x).CustomAsync(async (request, context, cancellation) =>
            {
                if (!request.Capacity.HasValue)
                {
                    return;
                }

                var activity = await dbContext.Activities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == request.ActivityId, cancellation);
                if (activity != null && request.Capacity.Value > activity.Capacity)
                {
                    context.AddFailure(
                        nameof(ScheduleUpdateRequest.Capacity),
                        $"Capacity cannot exceed the activity capacity ({activity.Capacity}).");
                }
            });
        }
    }
}
