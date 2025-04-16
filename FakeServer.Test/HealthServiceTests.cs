using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace FakeServer.Health
{
    public class HealthServiceTests
    {

        private readonly WebApplicationFactory<Program> _factory;

        public HealthServiceTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetHealthStatusAsync_ShouldReturnHealthyStatus_WhenServiceIsHealthy()
        {
            // Arrange
            var mockTimeProvider = new Mock<IServerTimeProvider>();
            var serverStartTime = DateTimeOffset.UtcNow.AddHours(-3).AddMinutes(-15); // simulate uptime
            mockTimeProvider.Setup(p => p.ServerStartTime).Returns(serverStartTime);

            var mockLogger = Mock.Of<ILogger<HealthCheckService>>();
            var service = new HealthCheckService(mockTimeProvider.Object, mockLogger);

            // Act
            var result = await service.GetHealthStatusAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Contains("3 hours", result.Uptime); // loosen match if needed
            Assert.Equal("1.0.0", result.Version);
        }

        [Fact]
        public async Task GetHealthStatusAsync_ShouldReturnUnhealthyStatus_WhenExceptionOccurs()
        {
            // Arrange
            var faultyProvider = new Mock<IServerTimeProvider>();
            faultyProvider.Setup(p => p.ServerStartTime).Throws(new Exception("Simulated failure"));

            var mockLogger = Mock.Of<ILogger<HealthCheckService>>();
            var service = new HealthCheckService(faultyProvider.Object, mockLogger);

            // Act
            var result = await service.GetHealthStatusAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Null(result.Uptime);
            Assert.Null(result.Version);
        }

        [Fact]
        public void ServerTimeProvider_ShouldReturnConsistentStartTime()
        {
            // Arrange
            var provider = new ServerTimeProvider();

            // Act
            var first = provider.ServerStartTime;
            Task.Delay(100).Wait(); // simulate delay
            var second = provider.ServerStartTime;

            // Assert
            Assert.Equal(first, second);
        }

        [Fact]
        public async Task HealthEndpoint_ReturnsHealthyStatus()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/health");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("Healthy", content);
        }

        private class FaultyHealthCheckService : IHealthCheckService
        {
            public Task<HealthStatus> GetHealthStatusAsync()
            {
                throw new Exception("Simulated failure");
            }
        }
    }
}
