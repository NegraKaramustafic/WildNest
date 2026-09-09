using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Authorize]
[Route("api/UserBadge")]
public class UserBadgeController : BaseReadController<UserBadgeResponse, UserBadgeSearchObject, IUserBadgeService>
{
    public UserBadgeController(IUserBadgeService userBadgeService) : base(userBadgeService)
    {
    }
}
