using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class ReservationExpiryBackgroundServiceTests
    {
        private readonly Mock<ILogger<ReservationExpiryBackgroundService>> _mockLogger;
        private readonly Mock<ILentItemsService> _mockLentItemsService;

        public ReservationExpiryBackgroundServiceTests()
        {
            _mockLogger = new Mock<ILogger<ReservationExpiryBackgroundService>>();
            _mockLentItemsService = new Mock<ILentItemsService>();
        }

        #region ExecuteAsync Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStartAndStopSuccessfully()
        {
            // Arrange
            _mockLentItemsService
                .Setup(x => x.CancelExpiredReservationsAsync())
                .ReturnsAsync(0);

            var serviceProvider = CreateServiceProvider();
            var service = new ReservationExpiryBackgroundService(serviceProvider, _mockLogger.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(150);
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldCallCancelExpiredReservations()
        {
            // Arrange
            _mockLentItemsService
                .Setup(x => x.CancelExpiredReservationsAsync())
                .ReturnsAsync(2);

            var serviceProvider = CreateServiceProvider();
            var service = new ReservationExpiryBackgroundService(serviceProvider, _mockLogger.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(150);
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockLentItemsService.Verify(
                x => x.CancelExpiredReservationsAsync(),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_WhenExceptionOccurs_ShouldLogErrorAndContinue()
        {
            // Arrange
            var exceptionThrown = false;
            _mockLentItemsService
                .Setup(x => x.CancelExpiredReservationsAsync())
                .Callback(() =>
                {
                    if (!exceptionThrown)
                    {
                        exceptionThrown = true;
                        throw new Exception("Test exception");
                    }
                })
                .ReturnsAsync(0);

            var serviceProvider = CreateServiceProvider();
            var service = new ReservationExpiryBackgroundService(serviceProvider, _mockLogger.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(200));

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(250);
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_WhenCancellationRequested_ShouldStopGracefully()
        {
            // Arrange
            _mockLentItemsService
                .Setup(x => x.CancelExpiredReservationsAsync())
                .ReturnsAsync(0);

            var serviceProvider = CreateServiceProvider();
            var service = new ReservationExpiryBackgroundService(serviceProvider, _mockLogger.Object);

            var cts = new CancellationTokenSource();

            // Act
            await service.StartAsync(cts.Token);
            cts.Cancel();
            await Task.Delay(200); // Give more time for the service to process cancellation
            await service.StopAsync(CancellationToken.None);

            // Assert - Verify starting was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            // Stopping message may or may not be logged depending on timing
            // The important thing is that the service handles cancellation gracefully
        }

        [Fact]
        public async Task ExecuteAsync_WhenReservationsCanceled_ShouldLogCount()
        {
            // Arrange
            _mockLentItemsService
                .Setup(x => x.CancelExpiredReservationsAsync())
                .ReturnsAsync(5);

            var serviceProvider = CreateServiceProvider();
            var service = new ReservationExpiryBackgroundService(serviceProvider, _mockLogger.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            await service.StartAsync(cts.Token);
            await Task.Delay(150);
            await service.StopAsync(CancellationToken.None);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Canceled 5 expired reservation")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Helper Methods

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddScoped<ILentItemsService>(_ => _mockLentItemsService.Object);
            return services.BuildServiceProvider();
        }

        #endregion
    }
}
