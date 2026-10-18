using eCommerce.Model.Access;
using eCommerce.Model.Requests;
using eCommerce.Services;
using eCommerce.WebAPI.Services;
using eCommerce.WebAPI.Services.AccessManager;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace eCommerce.WebAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AccessController : Controller
    {
        private readonly IAccessManager _accessManager;
        private readonly IUserService _userService;
        private readonly IPasswordResetService _passwordResetService;

        public AccessController(
            IAccessManager accessManager,
            IUserService userService,
            IPasswordResetService passwordResetService)
        {
            _accessManager = accessManager;
            _userService = userService;
            _passwordResetService = passwordResetService;
        }

        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicy.PolicyName)]
        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] UserLoginRequest request)
        {
            var result = await _accessManager.LoginAsync(request);
            return Ok(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicy.PolicyName)]
        [HttpPost("LoginWithRefreshToken")]
        public async Task<ActionResult> LoginWithRefreshToken([FromBody] RefreshAccessTokenRequest request)
        {
            var result = await _accessManager.LoginWithRefreshTokenAsync(request);
            return Ok(result);
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] UserInsertRequest request)
        {
            await _userService.InsertAsync(request);
            return Ok("You have registered successfully.");
        }

        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicy.PolicyName)]
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _passwordResetService.RequestAsync(request, cancellationToken);
            return Ok(result);
        }

        [AllowAnonymous]
        [EnableRateLimiting(AuthRateLimitPolicy.PolicyName)]
        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            await _passwordResetService.ResetAsync(request, cancellationToken);
            return Ok(new { message = PasswordResetMessages.ResetSuccess });
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await _accessManager.LogoutAsync();
            return Ok();
        }
    }
}
