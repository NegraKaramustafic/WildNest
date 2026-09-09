using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[Route("api/Donation")]
public class DonationController : BaseCRUDController<DonationResponse, DonationSearchObject, DonationInsertRequest, DonationUpdateRequest, IDonationService>
{
    public DonationController(IDonationService donationService) : base(donationService)
    {
    }
}
