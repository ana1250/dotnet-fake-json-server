using System;
using System.Threading.Tasks;

namespace FakeServer.Health
{
    public interface IHealthCheckService
    {
        Task<HealthStatus> GetHealthStatusAsync();
    }
}
