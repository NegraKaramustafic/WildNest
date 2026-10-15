using eCommerce.Model.Requests;
using eCommerce.Services.Database;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace eCommerce.Services.Validators
{
    public class ScheduleUpdateValidator : AbstractValidator<ScheduleUpdateRequest>
    {
        public ScheduleUpdateValidator(ECommerceDbContext dbContext)
        {
            RuleFor(x => x.ActivityId)
                .GreaterThan(0).WithMessage("Activity is required.");

            RuleFor(x => x.Date)
                .Must(date => date != default)
                .WithMessage("Date is required.");

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
                        nameof(ScheduleUpdateRequest.CapacityLeft),
                        $"Remaining capacity cannot exceed the activity capacity ({activity.Capacity}).");
                }
            });
        }
    }
}
