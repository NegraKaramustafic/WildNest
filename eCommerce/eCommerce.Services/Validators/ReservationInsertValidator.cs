using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ReservationInsertValidator : AbstractValidator<ReservationInsertRequest>
    {
        public ReservationInsertValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId is required and must be greater than 0.");

            RuleFor(x => x.ScheduleId)
                .GreaterThan(0).WithMessage("ScheduleId is required and must be greater than 0.");

            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage("GuestCount must be greater than 0.");
        }
    }
}
