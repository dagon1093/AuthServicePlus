using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServicePlus.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class AuthController: ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                await _authService.RegisterAsync(dto);
                return Ok(new { message = "Пользователь успешно зарегестрирован" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message});
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserDto dto)
        {
            try
            {
                var token = await _authService.LoginAsync(dto);
                return Ok(token);
            }
            catch(Exception ex) 
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
        {
            try
            {
                return Ok(await _authService.RefreshAsync(dto.RefreshToken));
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<object> Me()
        {
            var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);
            var expires = User.FindFirst("exp")?.Value;

            return Ok(new { userId, name, role, expires });

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto)
        {
            await _authService.LogoutAsync(dto.RefreshToken);
            return Ok(); //В любом случае ок
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            await _authService.LogoutAllAsync(userId);
            return Ok();
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var sessions = await _authService.GetSessionsAsync(userId);

            return Ok(sessions);
        }

        [Authorize]
        [HttpDelete("session/{id:int}")]
        public async Task<IActionResult> RevokeSession([FromRoute] int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

            var ok = await _authService.RevokeSessionAsync(userId, id);
            if(!ok) return NotFound(new { error = "Session not found"} );
            return Ok(ok);
        }


    }
}
