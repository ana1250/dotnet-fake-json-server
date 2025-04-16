namespace FakeServer.Health
{
    public class HealthStatus
    {
        public bool IsHealthy { get; set; }
        public string Uptime { get; set; }
        public string Version { get; set; }
    }
}
