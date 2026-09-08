using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class PaymentUpdateValidator : AbstractValidator<PaymentUpdateRequest>
    {
        public PaymentUpdateValidator()
        {
            RuleFor(x => x.Method)
                .IsInEnum().WithMessage("Method is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0.");

            RuleFor(x => x.StripePaymentIntentId)
                .MaximumLength(255).When(x => !string.IsNullOrWhiteSpace(x.StripePaymentIntentId))
                .WithMessage("StripePaymentIntentId cannot exceed 255 characters.");
        }
    }
}
