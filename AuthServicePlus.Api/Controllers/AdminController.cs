using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AuthServicePlus.Api.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController: ControllerBase
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        [Authorize(Roles ="Admin")]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            _logger.LogInformation("Поступил административный запрос ping");
            var response = new { Ok = true, at = DateTime.UtcNow };
            _logger.LogInformation("Возвращаем ответ на административный ping");
            return Ok(response);
        }
    }
}
