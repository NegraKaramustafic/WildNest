using eCommerce.Model;
using eCommerce.Model.Responses;
using eCommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.WebAPI.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduleReminderController : ControllerBase
    {
        private readonly IScheduleReminderService _service;

        public ScheduleReminderController(IScheduleReminderService service)
        {
            _service = service;
        }

        [HttpPost("run")]
        public async Task<ActionResult<ScheduleReminderRunResponse>> Run(CancellationToken cancellationToken)
        {
            var sent = await _service.SendDueRemindersAsync(cancellationToken);
            return Ok(new ScheduleReminderRunResponse { Sent = sent });
        }
    }
}
