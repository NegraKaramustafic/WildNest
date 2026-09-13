using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class DonationInsertValidator : AbstractValidator<DonationInsertRequest>
    {
        public DonationInsertValidator()
        {
            RuleFor(x => x.ZooPartnerId)
                .GreaterThan(0).WithMessage("ZooPartnerId is required and must be greater than 0.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than 0.");
        }
    }
}
