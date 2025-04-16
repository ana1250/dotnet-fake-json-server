using FakeServer.Health;

public class HealthCheckService : IHealthCheckService
{
    private const string ServerVersion = "1.0.0";

    private readonly IServerTimeProvider _serverTimeProvider;
    private readonly ILogger<HealthCheckService> _logger;

    public HealthCheckService(IServerTimeProvider serverTimeProvider, ILogger<HealthCheckService> logger)
    {
        _serverTimeProvider = serverTimeProvider;
        _logger = logger;
    }
    public Task<HealthStatus> GetHealthStatusAsync()
    {
        try
        {
            var uptime = DateTime.UtcNow - _serverTimeProvider.ServerStartTime;
            var uptimeString = $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";

            return Task.FromResult(new HealthStatus
            {
                IsHealthy = true,
                Uptime = uptimeString,
                Version = ServerVersion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed.");
            return Task.FromResult(new HealthStatus { IsHealthy = false });
        }
    }
}
