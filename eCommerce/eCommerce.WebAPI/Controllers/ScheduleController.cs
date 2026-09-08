using eCommerce.Model.Requests;
using eCommerce.Model.Responses;
using eCommerce.Model.SearchObjects;
using eCommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers;

[Route("api/Schedule")]
public class ScheduleController : BaseCRUDController<ScheduleResponse, ScheduleSearchObject, ScheduleInsertRequest, ScheduleUpdateRequest, IScheduleService>
{
    public ScheduleController(IScheduleService scheduleService) : base(scheduleService)
    {
    }
}
