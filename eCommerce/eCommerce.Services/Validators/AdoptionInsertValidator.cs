using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class AdoptionInsertValidator : AbstractValidator<AdoptionInsertRequest>
    {
        public AdoptionInsertValidator()
        {
            RuleFor(x => x.AnimalId)
                .GreaterThan(0).WithMessage("AnimalId is required and must be greater than 0.");
        }
    }
}
