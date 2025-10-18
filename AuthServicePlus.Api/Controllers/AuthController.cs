using AuthServicePlus.Application.DTOs;
using AuthServicePlus.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Security.Claims;

namespace AuthServicePlus.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class AuthController: ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                _logger.LogInformation("Запрос регистрации для пользователя {Username}", dto.Username);
                await _authService.RegisterAsync(dto);
                _logger.LogInformation("Пользователь {Username} успешно зарегистрирован", dto.Username);
                return Ok(new { message = "Пользователь успешно зарегестрирован" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка регистрации пользователя {Username}", dto.Username);
                return BadRequest(new { error = ex.Message});
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginUserDto dto)
        {
            try
            {
                _logger.LogInformation("Попытка входа для пользователя {Username}", dto.Username);
                var token = await _authService.LoginAsync(dto);
                _logger.LogInformation("Пользователь {Username} успешно вошёл", dto.Username);
                return Ok(token);
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Неуспешная попытка входа для пользователя {Username}", dto.Username);
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshRequestDto dto)
        {
            try
            {
                _logger.LogInformation("Попытка обновления токена");
                var response = await _authService.RefreshAsync(dto.RefreshToken);
                _logger.LogInformation("Токен успешно обновлён");
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Неуспешная попытка обновления токена");
                return Unauthorized(new { error = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("me")]
        public ActionResult<object> Me()
        {
            _logger.LogInformation("Получение информации о текущем пользователе");
            var userId  = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var role = User.FindFirstValue(ClaimTypes.Role);
            var expires = User.FindFirst("exp")?.Value;

            _logger.LogInformation("Информация о пользователе получена");
            return Ok(new { userId, name, role, expires });

        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequestDto dto)
        {
            _logger.LogInformation("Выход пользователя с указанным refresh токеном");
            await _authService.LogoutAsync(dto.RefreshToken);
            _logger.LogInformation("Refresh токен отозван");
            return Ok(); //В любом случае ок
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            _logger.LogInformation("Выход пользователя {UserId} со всех сессий", userId);
            await _authService.LogoutAllAsync(userId);
            _logger.LogInformation("Все сессии пользователя {UserId} завершены", userId);
            return Ok();
        }

        [Authorize]
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            _logger.LogInformation("Запрос списка сессий пользователя {UserId}", userId);
            var sessions = (await _authService.GetSessionsAsync(userId)).ToList();
            _logger.LogInformation("Получено {SessionsCount} сессий пользователя {UserId}", sessions.Count, userId);

            return Ok(sessions);
        }

        [Authorize]
        [HttpDelete("sessions/{id:int}")]
        public async Task<IActionResult> RevokeSession([FromRoute] int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out var userId)) return Unauthorized();

            _logger.LogInformation("Попытка отзыва сессии {SessionId} пользователем {UserId}", id, userId);
            var ok = await _authService.RevokeSessionAsync(userId, id);
            if(!ok)
            {
                _logger.LogWarning("Сессия {SessionId} для пользователя {UserId} не найдена", id, userId);
                return NotFound(new { error = "Session not found"} );
            }
            _logger.LogInformation("Сессия {SessionId} для пользователя {UserId} отозвана", id, userId);
            return Ok(ok);
        }


    }
}
