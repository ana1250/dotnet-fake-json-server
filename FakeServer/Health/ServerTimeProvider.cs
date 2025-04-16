namespace FakeServer.Health
{
    public class ServerTimeProvider : IServerTimeProvider
    {
        public DateTimeOffset ServerStartTime { get; }

        public ServerTimeProvider()
        {
            ServerStartTime = DateTimeOffset.UtcNow; // Important: don't forget this!
        }
    }
}
