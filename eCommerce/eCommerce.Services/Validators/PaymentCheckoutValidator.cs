using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class PaymentCheckoutValidator : AbstractValidator<PaymentCheckoutRequest>
    {
        public PaymentCheckoutValidator()
        {
            RuleFor(x => x.RelatedEntityType)
                .IsInEnum().WithMessage("RelatedEntityType must be Ticket, Reservation, Adoption, or Donation.");

            RuleFor(x => x.RelatedEntityId)
                .GreaterThan(0).WithMessage("RelatedEntityId is required and must be greater than 0.");
        }
    }
}
