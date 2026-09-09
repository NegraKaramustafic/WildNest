using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class AdoptionUpdateValidator : AbstractValidator<AdoptionUpdateRequest>
    {
        public AdoptionUpdateValidator()
        {
            RuleFor(x => x.MonthlyAmount)
                .GreaterThan(0).WithMessage("MonthlyAmount must be greater than 0.");
        }
    }
}
