using BackendTechnicalAssetsManagement.src.Exceptions;
using BackendTechnicalAssetsManagement.src.Middleware;
using BackendTechnicalAssetsManagement.src.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Middleware
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<RequestDelegate> _mockNext;
        private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
        private readonly GlobalExceptionHandler _handler;

        public GlobalExceptionHandlerTests()
        {
            _mockNext = new Mock<RequestDelegate>();
            _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
            _handler = new GlobalExceptionHandler(_mockNext.Object, _mockLogger.Object);
        }

        #region General Exception Handling Tests

        [Fact]
        public async Task InvokeAsync_WithNoException_ShouldCallNext()
        {
            // Arrange
            var context = new DefaultHttpContext();
            _mockNext.Setup(x => x(context)).Returns(Task.CompletedTask);

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            _mockNext.Verify(x => x(context), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithGeneralException_ShouldReturn500()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new Exception("Unexpected error"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.InternalServerError, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithGeneralException_ShouldLogError()
        {
            // Arrange
            var context = CreateHttpContext();
            var exception = new Exception("Test error");
            _mockNext.Setup(x => x(context)).ThrowsAsync(exception);

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unhandled exception")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region RefreshTokenException Handling Tests

        [Fact]
        public async Task InvokeAsync_WithRefreshTokenException_ShouldReturn401()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new RefreshTokenException("Token expired"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.Unauthorized, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithRefreshTokenException_ShouldReturnErrorMessage()
        {
            // Arrange
            var context = CreateHttpContext();
            var errorMessage = "Refresh token has expired";
            _mockNext.Setup(x => x(context)).ThrowsAsync(new RefreshTokenException(errorMessage));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            var responseBody = await GetResponseBody(context);
            Assert.Contains(errorMessage, responseBody);
        }

        #endregion

        #region Validation Exception Handling Tests

        [Fact]
        public async Task InvokeAsync_WithArgumentException_ShouldReturn400()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new ArgumentException("Invalid argument"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidOperationException_ShouldReturn400()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        }

        #endregion

        #region Unauthorized Exception Handling Tests

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldReturn403()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new UnauthorizedAccessException("Access denied"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.Forbidden, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithUnauthorizedAccessException_ShouldReturnErrorMessage()
        {
            // Arrange
            var context = CreateHttpContext();
            var errorMessage = "You do not have permission";
            _mockNext.Setup(x => x(context)).ThrowsAsync(new UnauthorizedAccessException(errorMessage));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            var responseBody = await GetResponseBody(context);
            Assert.Contains(errorMessage, responseBody);
        }

        #endregion

        #region Not Found Exception Handling Tests

        [Fact]
        public async Task InvokeAsync_WithKeyNotFoundException_ShouldReturn404()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new KeyNotFoundException("Resource not found"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            Assert.Equal((int)HttpStatusCode.NotFound, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithKeyNotFoundException_ShouldReturnErrorMessage()
        {
            // Arrange
            var context = CreateHttpContext();
            var errorMessage = "User not found";
            _mockNext.Setup(x => x(context)).ThrowsAsync(new KeyNotFoundException(errorMessage));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            var responseBody = await GetResponseBody(context);
            Assert.Contains(errorMessage, responseBody);
        }

        #endregion

        #region Response Format Tests

        [Fact]
        public async Task InvokeAsync_WithException_ShouldReturnApiResponseFormat()
        {
            // Arrange
            var context = CreateHttpContext();
            _mockNext.Setup(x => x(context)).ThrowsAsync(new ArgumentException("Test error"));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            var responseBody = await GetResponseBody(context);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);
            
            Assert.NotNull(apiResponse);
            Assert.False(apiResponse.Success);
            Assert.NotNull(apiResponse.Message);
        }

        [Fact]
        public async Task InvokeAsync_WithGeneralException_ShouldIncludeErrorList()
        {
            // Arrange
            var context = CreateHttpContext();
            var errorMessage = "Database connection failed";
            _mockNext.Setup(x => x(context)).ThrowsAsync(new Exception(errorMessage));

            // Act
            await _handler.InvokeAsync(context);

            // Assert
            var responseBody = await GetResponseBody(context);
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody);
            
            Assert.NotNull(apiResponse);
            Assert.NotNull(apiResponse.Errors);
            Assert.Contains(errorMessage, apiResponse.Errors);
        }

        #endregion

        #region Helper Methods

        private HttpContext CreateHttpContext()
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            return context;
        }

        private async Task<string> GetResponseBody(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            return await reader.ReadToEndAsync();
        }

        #endregion
    }
}
