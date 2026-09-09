using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class AdoptionInsertValidator : AbstractValidator<AdoptionInsertRequest>
    {
        public AdoptionInsertValidator()
        {
            RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("UserId is required and must be greater than 0.");

            RuleFor(x => x.AnimalId)
                .GreaterThan(0).WithMessage("AnimalId is required and must be greater than 0.");

            RuleFor(x => x.MonthlyAmount)
                .GreaterThan(0).When(x => x.MonthlyAmount.HasValue)
                .WithMessage("MonthlyAmount must be greater than 0.");
        }
    }
}
