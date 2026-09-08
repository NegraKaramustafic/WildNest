using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ReservationUpdateValidator : AbstractValidator<ReservationUpdateRequest>
    {
        public ReservationUpdateValidator()
        {
            RuleFor(x => x.GuestCount)
                .GreaterThan(0).WithMessage("GuestCount must be greater than 0.");
        }
    }
}
