using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/ZooPartner")]
public class ZooPartnerController : BaseCRUDController<ZooPartnerResponse, ZooPartnerSearchObject, ZooPartnerInsertRequest, ZooPartnerUpdateRequest, IZooPartnerService>
{
    public ZooPartnerController(IZooPartnerService zooPartnerService) : base(zooPartnerService)
    {
    }
}
