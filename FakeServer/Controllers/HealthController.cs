using FakeServer.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FakeServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly IHealthCheckService _healthCheckService;

        public HealthController(IHealthCheckService healthCheckService)
        {
            _healthCheckService = healthCheckService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(HealthStatus), 200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetHealth()
        {
            var healthStatus = await _healthCheckService.GetHealthStatusAsync();

            if (healthStatus.IsHealthy)
            {
                return Ok(new
                {
                    status = "Healthy",
                    uptime = healthStatus.Uptime,
                    version = healthStatus.Version
                });
            }

            return StatusCode(503, new { status = "Unhealthy" });
        }
    }
}
