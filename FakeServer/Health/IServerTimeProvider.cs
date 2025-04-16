namespace FakeServer.Health
{
    public interface IServerTimeProvider
    {

        DateTimeOffset ServerStartTime { get; }
    }
}
