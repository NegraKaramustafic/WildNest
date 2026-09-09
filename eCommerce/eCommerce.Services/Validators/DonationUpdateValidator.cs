using eCommerce.Model.Requests;
using FluentValidation;

namespace eCommerce.Services.Validators
{
    public class DonationUpdateValidator : AbstractValidator<DonationUpdateRequest>
    {
        public DonationUpdateValidator()
        {
        }
    }
}
