using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.Exceptions;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Security.Claims;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class AuthServiceTests
    {
        private readonly AppDbContext _context;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IUserValidationService> _mockUserValidationService;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<IDevelopmentLoggerService> _mockDevelopmentLoggerService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Lightweight in-memory DbContext (not used in refactored methods, but required by constructor)
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            
            _mockConfiguration = new Mock<IConfiguration>();
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockMapper = new Mock<IMapper>();
            _mockPasswordHashingService = new Mock<IPasswordHashingService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockUserValidationService = new Mock<IUserValidationService>();
            _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockDevelopmentLoggerService = new Mock<IDevelopmentLoggerService>();

            // Setup default configuration for JWT token
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x.Value).Returns("ThisIsASecretKeyForJWTTokenGenerationThatIsAtLeast32CharactersLong");
            _mockConfiguration.Setup(x => x.GetSection("AppSettings:Token")).Returns(mockConfigSection.Object);

            // Setup environment as Development by default
            _mockEnv.Setup(x => x.EnvironmentName).Returns("Development");

            // Initialize the service with all mocks
            _authService = new AuthService(
                _context,
                _mockConfiguration.Object,
                _mockHttpContextAccessor.Object,
                _mockPasswordHashingService.Object,
                _mockUserRepository.Object,
                _mockMapper.Object,
                _mockUserValidationService.Object,
                _mockEnv.Object,
                _mockDevelopmentLoggerService.Object,
                _mockRefreshTokenRepository.Object
            );
        }

        #region Register Tests

        [Fact]
        public async Task Register_WithValidStudentData_ShouldCreateStudent()
        {
            // Arrange - Using Mock Data
            var registerDto = AuthMockData.GetValidStudentRegisterDto();
            var newStudent = AuthMockData.GetMockStudent();
            var userDto = AuthMockData.GetMockUserDto(newStudent.Id, newStudent.Username, newStudent.Email);

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(registerDto.Username, registerDto.Email, registerDto.PhoneNumber))
                .Returns(Task.FromResult(0));

            _mockMapper
                .Setup(x => x.Map(registerDto, It.IsAny<Student>()))
                .Callback<RegisterUserDto, Student>((src, dest) =>
                {
                    dest.Username = src.Username;
                    dest.Email = src.Email;
                });

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(registerDto.Password))
                .Returns("hashedPassword123");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(newStudent);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(userDto);

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(registerDto.Username, result.Username);
            Assert.Equal(registerDto.Email, result.Email);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Register_WithInvalidPassword_ShouldThrowArgumentException()
        {
            // Arrange - Using Mock Data
            var registerDto = AuthMockData.GetInvalidPasswordRegisterDto();

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.Register(registerDto));
        }

        [Theory]
        [InlineData("short")] // Too short
        [InlineData("nouppercase123!")] // No uppercase
        [InlineData("NOLOWERCASE123!")] // No lowercase
        [InlineData("NoNumbers!")] // No numbers
        [InlineData("NoSpecialChar123")] // No special characters
        public async Task Register_WithVariousInvalidPasswords_ShouldThrowArgumentException(string invalidPassword)
        {
            // Arrange - Using Mock Data with custom password
            var registerDto = AuthMockData.GetInvalidPasswordRegisterDto();
            registerDto.Password = invalidPassword;

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.FromResult(0));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.Register(registerDto));
        }

        #endregion

        #region Login Tests

        // COMMENTED OUT: This test is slow due to DbContext operations
        // Uncomment when you need to test the full login flow with cookies
        //[Fact]
        //public async Task Login_WithValidCredentials_ShouldReturnUserDto()
        //{
        //    // Arrange - Using Mock Data
        //    var loginDto = AuthMockData.GetValidLoginDto();
        //    var user = AuthMockData.GetMockUser();
        //    var userDto = AuthMockData.GetMockUserDto(user.Id, user.Username);

        //    _mockUserRepository
        //        .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
        //        .ReturnsAsync(user);

        //    _mockPasswordHashingService
        //        .Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
        //        .Returns(true);

        //    _mockMapper
        //        .Setup(x => x.Map<UserDto>(user))
        //        .Returns(userDto);

        //    var mockHttpContext = new Mock<HttpContext>();
        //    var mockResponse = new Mock<HttpResponse>();
        //    var mockCookies = new Mock<IResponseCookies>();

        //    mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
        //    mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
        //    _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

        //    // Act
        //    var result = await _authService.Login(loginDto);

        //    // Assert
        //    Assert.NotNull(result);
        //    Assert.Equal(user.Username, result.Username);
        //    _mockPasswordHashingService.Verify(x => x.VerifyPassword(loginDto.Password, user.PasswordHash), Times.Once);
        //}

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnUserDto_UsingMockData()
        {
            // Arrange - Using Mock Data (Fast unit test - fully mocked, no DbContext!)
            var loginDto = AuthMockData.GetValidLoginDto();
            var user = AuthMockData.GetMockUser();
            var userDto = AuthMockData.GetMockUserDto(user.Id, user.Username, user.Email);

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync(user);

            _mockPasswordHashingService
                .Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);

            _mockMapper
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Mock refresh token repository operations
            _mockRefreshTokenRepository
                .Setup(x => x.RevokeAllForUserAsync(user.Id))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Mock HttpContext for cookies (minimal setup)
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();

            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Act
            var result = await _authService.Login(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.Email, result.Email);
            
            // Verify all method calls
            _mockUserRepository.Verify(x => x.GetByIdentifierAsync(loginDto.Identifier), Times.Once);
            _mockPasswordHashingService.Verify(x => x.VerifyPassword(loginDto.Password, user.PasswordHash), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(user.Id), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mockMapper.Verify(x => x.Map<UserDto>(user), Times.Once);
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ShouldThrowException()
        {
            // Arrange - Using Mock Data
            var loginDto = AuthMockData.GetInvalidUsernameLoginDto();

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.Login(loginDto));
            Assert.Equal("Invalid username or password.", exception.Message);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldThrowException()
        {
            // Arrange - Using Mock Data
            var loginDto = AuthMockData.GetInvalidPasswordLoginDto();
            var user = AuthMockData.GetMockUser();

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync(user);

            _mockPasswordHashingService
                .Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.Login(loginDto));
            Assert.Equal("Invalid username or password.", exception.Message);
        }

        #endregion

        #region LoginMobile Tests

        [Fact]
        public async Task LoginMobile_WithValidCredentials_ShouldReturnMobileLoginResponseDto()
        {
            // Arrange - Using Mock Data (Fast unit test - fully mocked!)
            var loginDto = AuthMockData.GetValidLoginDto();
            var user = AuthMockData.GetMockUser();
            var userDto = AuthMockData.GetMockUserDto(user.Id, user.Username, user.Email);

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync(user);

            _mockPasswordHashingService
                .Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(true);

            _mockMapper
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Mock refresh token repository operations
            _mockRefreshTokenRepository
                .Setup(x => x.RevokeAllForUserAsync(user.Id))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.LoginMobile(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.Equal(user.Username, result.User.Username);
            Assert.Equal(user.Email, result.User.Email);
            
            // Verify tokens are not empty
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            
            // Verify method calls
            _mockUserRepository.Verify(x => x.GetByIdentifierAsync(loginDto.Identifier), Times.Once);
            _mockPasswordHashingService.Verify(x => x.VerifyPassword(loginDto.Password, user.PasswordHash), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(user.Id), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
            _mockMapper.Verify(x => x.Map<UserDto>(user), Times.Once);
        }

        [Fact]
        public async Task LoginMobile_WithInvalidUsername_ShouldThrowException()
        {
            // Arrange - Using Mock Data
            var loginDto = AuthMockData.GetInvalidUsernameLoginDto();

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginMobile(loginDto));
            Assert.Equal("Invalid username or password.", exception.Message);
        }

        [Fact]
        public async Task LoginMobile_WithInvalidPassword_ShouldThrowException()
        {
            // Arrange - Using Mock Data
            var loginDto = AuthMockData.GetInvalidPasswordLoginDto();
            var user = AuthMockData.GetMockUser();

            _mockUserRepository
                .Setup(x => x.GetByIdentifierAsync(loginDto.Identifier))
                .ReturnsAsync(user);

            _mockPasswordHashingService
                .Setup(x => x.VerifyPassword(loginDto.Password, user.PasswordHash))
                .Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _authService.LoginMobile(loginDto));
            Assert.Equal("Invalid username or password.", exception.Message);
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_UserChangingOwnPassword_ShouldSucceed()
        {
            // Arrange - Using Mock Data
            var userId = Guid.NewGuid();
            var changePasswordDto = AuthMockData.GetValidChangePasswordDto();
            var user = AuthMockData.GetMockUser(userId);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);

            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);

            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(changePasswordDto.NewPassword))
                .Returns("newHashedPassword");

            _mockUserRepository
                .Setup(x => x.UpdateAsync(user))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.RevokeAllForUserAsync(userId))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _authService.ChangePassword(userId, changePasswordDto);

            // Assert
            _mockPasswordHashingService.Verify(x => x.HashPassword(changePasswordDto.NewPassword), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(user), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(userId), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_NonAdminChangingOtherUserPassword_ShouldThrowUnauthorizedException()
        {
            // Arrange - Using Mock Data
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid(); // Different user
            var changePasswordDto = AuthMockData.GetValidChangePasswordDto();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                new Claim(ClaimTypes.Role, "Student") // Not Admin
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authService.ChangePassword(targetUserId, changePasswordDto));
            Assert.Equal("You do not have permission to change passwords for other users.", exception.Message);
        }

        #endregion
    }
}
