using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CEX_DEX_Parser.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet, HttpHead]  // accepts both GET and HEAD
        public IActionResult Get() => Ok("healthy");
    }
}
