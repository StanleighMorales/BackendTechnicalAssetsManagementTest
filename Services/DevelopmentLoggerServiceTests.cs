using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class DevelopmentLoggerServiceTests
    {
        private readonly Mock<ILogger<DevelopmentLoggerService>> _mockLogger;
        private readonly DevelopmentLoggerService _developmentLoggerService;

        public DevelopmentLoggerServiceTests()
        {
            _mockLogger = new Mock<ILogger<DevelopmentLoggerService>>();
            _developmentLoggerService = new DevelopmentLoggerService(_mockLogger.Object);
        }

        #region LogTokenSent Tests

        [Fact]
        public void LogTokenSent_WithValidParameters_ShouldLogWarning()
        {
            // Arrange
            var expiryDuration = TimeSpan.FromMinutes(15);
            var tokenType = "Access";

            // Act
            _developmentLoggerService.LogTokenSent(expiryDuration, tokenType);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TOKEN SENT") && 
                                                   v.ToString()!.Contains("Access") &&
                                                   v.ToString()!.Contains("15m")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogTokenSent_WithRefreshToken_ShouldLogCorrectTokenType()
        {
            // Arrange
            var expiryDuration = TimeSpan.FromDays(7);
            var tokenType = "Refresh";

            // Act
            _developmentLoggerService.LogTokenSent(expiryDuration, tokenType);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Refresh")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData(15, "Access")]
        [InlineData(30, "Refresh")]
        [InlineData(60, "Custom")]
        public void LogTokenSent_WithVariousDurations_ShouldLogCorrectly(int minutes, string tokenType)
        {
            // Arrange
            var expiryDuration = TimeSpan.FromMinutes(minutes);

            // Act
            _developmentLoggerService.LogTokenSent(expiryDuration, tokenType);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"{minutes}m")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region LogTokenAlmostExpired Tests

        [Fact]
        public async Task LogTokenAlmostExpired_WithValidParameters_ShouldLogWarningAfterDelay()
        {
            // Arrange
            var tokenType = "Access";
            var expiryDuration = TimeSpan.FromSeconds(2);
            var threshold = TimeSpan.FromSeconds(1);

            // Act
            _developmentLoggerService.LogTokenAlmostExpired(tokenType, expiryDuration, threshold);

            // Wait for the delayed task to execute
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TOKEN WARNING") && 
                                                   v.ToString()!.Contains("Access")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogTokenAlmostExpired_WhenExpiryLessThanThreshold_ShouldLogImmediately()
        {
            // Arrange
            var tokenType = "Access";
            var expiryDuration = TimeSpan.FromSeconds(10);
            var threshold = TimeSpan.FromSeconds(30); // Threshold greater than expiry

            // Act
            _developmentLoggerService.LogTokenAlmostExpired(tokenType, expiryDuration, threshold);

            // Assert - Should log immediately
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("nearing expiry now")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogTokenAlmostExpired_WithZeroExpiry_ShouldLogImmediately()
        {
            // Arrange
            var tokenType = "Access";
            var expiryDuration = TimeSpan.Zero;
            var threshold = TimeSpan.FromSeconds(30);

            // Act
            _developmentLoggerService.LogTokenAlmostExpired(tokenType, expiryDuration, threshold);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("nearing expiry now")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void LogTokenAlmostExpired_WithNegativeExpiry_ShouldLogImmediately()
        {
            // Arrange
            var tokenType = "Access";
            var expiryDuration = TimeSpan.FromSeconds(-10);
            var threshold = TimeSpan.FromSeconds(30);

            // Act
            _developmentLoggerService.LogTokenAlmostExpired(tokenType, expiryDuration, threshold);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("nearing expiry now")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("Access")]
        [InlineData("Refresh")]
        [InlineData("Custom")]
        public void LogTokenAlmostExpired_WithDifferentTokenTypes_ShouldLogCorrectType(string tokenType)
        {
            // Arrange
            var expiryDuration = TimeSpan.FromSeconds(10);
            var threshold = TimeSpan.FromSeconds(30);

            // Act
            _developmentLoggerService.LogTokenAlmostExpired(tokenType, expiryDuration, threshold);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(tokenType)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task LogTokenSent_ShouldTriggerLogTokenAlmostExpired()
        {
            // Arrange
            var expiryDuration = TimeSpan.FromSeconds(2);
            var tokenType = "Access";

            // Act
            _developmentLoggerService.LogTokenSent(expiryDuration, tokenType);

            // Wait for delayed warning
            await Task.Delay(TimeSpan.FromSeconds(1.5));

            // Assert - Should have logged both TOKEN SENT and TOKEN WARNING
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TOKEN SENT")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TOKEN WARNING")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
