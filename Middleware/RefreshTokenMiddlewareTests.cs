using BackendTechnicalAssetsManagement.src.Exceptions;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Middleware
{
    public class RefreshTokenMiddlewareTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<ILogger<RefreshTokenMiddleware>> _mockLogger;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly RefreshTokenMiddleware _middleware;

        public RefreshTokenMiddlewareTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<RefreshTokenMiddleware>>();
            _mockAuthService = new Mock<IAuthService>();
            _middleware = new RefreshTokenMiddleware(_mockNext.Object, _mockLogger.Object);
        }

        #region Valid Token Processing Tests

        [Fact]
        public async Task InvokeAsync_WithValidTokenNotNearExpiry_ShouldNotRefresh()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 600); // 10 minutes

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Never);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithTokenNearExpiry_ShouldRefresh()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 5); // 5 seconds

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Once);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region Expired Token Handling Tests

        [Fact]
        public async Task InvokeAsync_WithExpiredTokenWithinBuffer_ShouldRefresh()
        {
            // Arrange - Token expired 3 seconds ago (within -5 second buffer)
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: -3);

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Once);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithExpiredTokenBeyondBuffer_ShouldNotRefresh()
        {
            // Arrange - Token expired 10 seconds ago (beyond -5 second buffer)
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: -10);

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Never);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region Invalid Token Handling Tests

        [Fact]
        public async Task InvokeAsync_WithInvalidExpirationClaim_ShouldNotRefresh()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, invalidExpClaim: true);

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Never);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region Missing Token Handling Tests

        [Fact]
        public async Task InvokeAsync_WithUnauthenticatedUser_ShouldNotRefresh()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: false);

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Never);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithMissingExpirationClaim_ShouldNotRefresh()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, noExpClaim: true);

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Never);
            _mockNext.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region Token Refresh Logic Tests

        [Fact]
        public async Task InvokeAsync_WithSuccessfulRefresh_ShouldLogInformation()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 5);
            _mockAuthService.Setup(x => x.RefreshToken()).ReturnsAsync("new-access-token");

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("successfully refreshed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task InvokeAsync_WithRefreshTokenException_ShouldReturn401()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 5);
            _mockAuthService.Setup(x => x.RefreshToken())
                .ThrowsAsync(new RefreshTokenException("Refresh token expired"));

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
            _mockNext.Verify(x => x(context), Times.Never);
        }

        [Fact]
        public async Task InvokeAsync_WithRefreshTokenException_ShouldLogWarning()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 5);
            _mockAuthService.Setup(x => x.RefreshToken())
                .ThrowsAsync(new RefreshTokenException("Refresh token expired"));

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Token refresh failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithGeneralException_ShouldLogErrorAndContinue()
        {
            // Arrange
            var context = CreateHttpContext(isAuthenticated: true, expiryInSeconds: 5);
            _mockAuthService.Setup(x => x.RefreshToken())
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            await _middleware.InvokeAsync(context, _mockAuthService.Object);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unexpected error")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            
            _mockNext.Verify(x => x(context), Times.Once);
        }

        #endregion

        #region Helper Methods

        private HttpContext CreateHttpContext(
            bool isAuthenticated = false,
            int expiryInSeconds = 600,
            bool invalidExpClaim = false,
            bool noExpClaim = false)
        {
            var context = new DefaultHttpContext();

            if (isAuthenticated)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                };

                if (!noExpClaim)
                {
                    var expTime = DateTimeOffset.UtcNow.AddSeconds(expiryInSeconds).ToUnixTimeSeconds();
                    var expValue = invalidExpClaim ? "invalid" : expTime.ToString();
                    claims.Add(new Claim("exp", expValue));
                }

                var identity = new ClaimsIdentity(claims, "TestAuth");
                context.User = new ClaimsPrincipal(identity);
            }

            return context;
        }

        #endregion
    }
}
