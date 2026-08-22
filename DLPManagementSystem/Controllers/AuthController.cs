using DLPManagementSystem.Authorization;
using DLPManagementSystem.Common;
using DLPManagementSystem.DTO.Auth;
using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DLPManagementSystem.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [Authorize]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
        {
            var response = await _authService.LoginAsync(request, cancellationToken);

            if (!response.Success)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpGet("me")]
        [AllowMustChangePassword]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var response = await _authService.GetCurrentUserAsync(userId, cancellationToken);

            if (!response.Success)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpPost("change-password")]
        [AllowMustChangePassword]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request, CancellationToken cancellationToken)
        {
            var userId = User.GetUserId();
            var response = await _authService.ChangePasswordAsync(userId, request, cancellationToken);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
