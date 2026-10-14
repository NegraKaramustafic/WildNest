using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class ActivityTypeInsertValidator : AbstractValidator<ActivityTypeInsertRequest>
    {
        public ActivityTypeInsertValidator()
        {
            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Naziv je obavezan.")
                .Must(name => name.Trim().Length >= 2).WithMessage("Naziv mora imati najmanje 2 znaka.")
                .Must(name => name.Trim().Length <= 50).WithMessage("Naziv ne može biti duži od 50 znakova.");

            RuleFor(x => x.Description)
                .Must(description => string.IsNullOrWhiteSpace(description) || description.Trim().Length <= 500)
                .WithMessage("Opis ne može biti duži od 500 znakova.");
        }
    }
}
