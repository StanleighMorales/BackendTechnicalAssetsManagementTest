using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockUserService = new Mock<IUserService>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            
            _controller = new AuthController(
                _mockAuthService.Object,
                _mockEnv.Object,
                _mockLogger.Object,
                _mockUserService.Object);
            
            // Setup HttpContext for cookie operations
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnOkWithUserDto()
        {
            // Arrange
            var loginDto = new LoginUserDto
            {
                Identifier = "testuser",
                Password = "Test@123"
            };

            var expectedUser = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com"
            };

            _mockAuthService
                .Setup(x => x.Login(loginDto))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockAuthService.Verify(x => x.Login(loginDto), Times.Once);
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ShouldReturnCreated()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var registerDto = new RegisterUserDto
            {
                Username = "newuser",
                Email = "newuser@test.com",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "New",
                LastName = "User",
                PhoneNumber = "1234567890",
                Role = UserRole.Student
            };

            var createdUser = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                Email = "newuser@test.com"
            };

            // Mock User claims
            var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, currentUserId.ToString())
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new System.Security.Claims.ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            _mockAuthService
                .Setup(x => x.Register(registerDto, currentUserId))
                .ReturnsAsync(createdUser);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var createdResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(201, createdResult.StatusCode);
            
            _mockAuthService.Verify(x => x.Register(registerDto, currentUserId), Times.Once);
        }

        #endregion

        #region RefreshToken Tests

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnOkWithNewToken()
        {
            // Arrange
            var newAccessToken = "new-access-token";
            _mockAuthService
                .Setup(x => x.RefreshToken())
                .ReturnsAsync(newAccessToken);

            // Act
            var result = await _controller.RefreshToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockAuthService.Verify(x => x.RefreshToken(), Times.Once);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_ShouldReturnOk()
        {
            // Arrange
            _mockAuthService
                .Setup(x => x.Logout())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockAuthService.Verify(x => x.Logout(), Times.Once);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var changePasswordDto = new ChangePasswordDto
            {
                NewPassword = "NewPass@123",
                ConfirmPassword = "NewPass@123"
            };

            _mockAuthService
                .Setup(x => x.ChangePassword(userId, changePasswordDto))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ChangePassword(userId, changePasswordDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockAuthService.Verify(
                x => x.ChangePassword(userId, changePasswordDto),
                Times.Once);
        }

        #endregion
    }
}
