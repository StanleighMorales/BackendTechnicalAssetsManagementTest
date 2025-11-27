using BackendTechnicalAssetsManagement.src.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class HealthControllerTests
    {
        private readonly Mock<HealthCheckService> _mockHealthCheckService;
        private readonly HealthController _controller;

        public HealthControllerTests()
        {
            _mockHealthCheckService = new Mock<HealthCheckService>();
            _controller = new HealthController(_mockHealthCheckService.Object);
        }

        #region Get Health Tests

        [Fact]
        public async Task Get_WhenHealthy_ShouldReturnOkWithHealthReport()
        {
            // Arrange
            var healthReport = new HealthReport(
                entries: new Dictionary<string, HealthReportEntry>
                {
                    ["Database"] = new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "Database is healthy",
                        duration: TimeSpan.FromMilliseconds(100),
                        exception: null,
                        data: null
                    )
                },
                totalDuration: TimeSpan.FromMilliseconds(100)
            );

            _mockHealthCheckService
                .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthReport);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<HealthReport>(okResult.Value);
            Assert.Equal(HealthStatus.Healthy, returnedReport.Status);
        }

        [Fact]
        public async Task Get_WhenUnhealthy_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var healthReport = new HealthReport(
                entries: new Dictionary<string, HealthReportEntry>
                {
                    ["Database"] = new HealthReportEntry(
                        status: HealthStatus.Unhealthy,
                        description: "Database connection failed",
                        duration: TimeSpan.FromMilliseconds(100),
                        exception: new Exception("Connection timeout"),
                        data: null
                    )
                },
                totalDuration: TimeSpan.FromMilliseconds(100)
            );

            _mockHealthCheckService
                .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthReport);

            // Act
            var result = await _controller.Get();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusCodeResult.StatusCode);
            var returnedReport = Assert.IsType<HealthReport>(statusCodeResult.Value);
            Assert.Equal(HealthStatus.Unhealthy, returnedReport.Status);
        }

        [Fact]
        public async Task Get_WhenDegraded_ShouldReturnServiceUnavailable()
        {
            // Arrange
            var healthReport = new HealthReport(
                entries: new Dictionary<string, HealthReportEntry>
                {
                    ["Cache"] = new HealthReportEntry(
                        status: HealthStatus.Degraded,
                        description: "Cache is slow",
                        duration: TimeSpan.FromMilliseconds(500),
                        exception: null,
                        data: null
                    )
                },
                totalDuration: TimeSpan.FromMilliseconds(500)
            );

            _mockHealthCheckService
                .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthReport);

            // Act
            var result = await _controller.Get();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task Get_WithMultipleHealthChecks_ShouldReturnOverallStatus()
        {
            // Arrange
            var healthReport = new HealthReport(
                entries: new Dictionary<string, HealthReportEntry>
                {
                    ["Database"] = new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "Database is healthy",
                        duration: TimeSpan.FromMilliseconds(50),
                        exception: null,
                        data: null
                    ),
                    ["API"] = new HealthReportEntry(
                        status: HealthStatus.Healthy,
                        description: "API is healthy",
                        duration: TimeSpan.FromMilliseconds(30),
                        exception: null,
                        data: null
                    )
                },
                totalDuration: TimeSpan.FromMilliseconds(80)
            );

            _mockHealthCheckService
                .Setup(x => x.CheckHealthAsync(It.IsAny<Func<HealthCheckRegistration, bool>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(healthReport);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedReport = Assert.IsType<HealthReport>(okResult.Value);
            Assert.Equal(HealthStatus.Healthy, returnedReport.Status);
            Assert.Equal(2, returnedReport.Entries.Count);
        }

        #endregion
    }
}
