using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServicePlus.Api.Controllers
{
    [ApiController]
    [Route("admin")]
    public class AdminController: ControllerBase
    {
        [Authorize(Roles ="Admin")]
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new {Ok = true, at = DateTime.UtcNow});
    }
}
