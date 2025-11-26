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
                .EnableSensitiveDataLogging(false) // Disable for performance
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
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

        #region RefreshToken Tests

        [Fact]
        public async Task RefreshToken_WithValidToken_ShouldReturnNewAccessToken()
        {
            // Arrange - Fully mocked (no DbContext)
            var userId = Guid.NewGuid();
            var user = AuthMockData.GetMockUser(userId);
            var oldRefreshToken = new RefreshToken
            {
                Token = "validRefreshToken123",
                UserId = userId,
                User = user,
                ExpiresAt = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                IsRevoked = false
            };

            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();

            mockRequestCookies.Setup(x => x["4CLC-Auth-SRT"]).Returns("validRefreshToken123");
            mockRequest.Setup(x => x.Cookies).Returns(mockRequestCookies.Object);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Mock repository to return the token
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("validRefreshToken123"))
                .ReturnsAsync(oldRefreshToken);

            _mockRefreshTokenRepository
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RefreshToken();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.True(oldRefreshToken.IsRevoked); // Verify old token was revoked
            Assert.NotNull(oldRefreshToken.RevokedAt);
            
            // Verify repository methods were called
            _mockRefreshTokenRepository.Verify(x => x.GetByTokenAsync("validRefreshToken123"), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WithMissingCookie_ShouldThrowRefreshTokenException()
        {
            // Arrange
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();

            mockRequestCookies.Setup(x => x["4CLC-Auth-SRT"]).Returns((string?)null);
            mockRequest.Setup(x => x.Cookies).Returns(mockRequestCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshToken());
            Assert.Contains("missing", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidToken_ShouldThrowRefreshTokenException()
        {
            // Arrange - Fully mocked
            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();

            mockRequestCookies.Setup(x => x["4CLC-Auth-SRT"]).Returns("invalidToken123");
            mockRequest.Setup(x => x.Cookies).Returns(mockRequestCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Mock repository to return null (token not found)
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("invalidToken123"))
                .ReturnsAsync((RefreshToken?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshToken());
            Assert.Contains("Invalid refresh token", exception.Message);
        }

        [Fact]
        public async Task RefreshToken_WithRevokedToken_ShouldThrowRefreshTokenException()
        {
            // Arrange - Fully mocked
            var userId = Guid.NewGuid();
            var user = AuthMockData.GetMockUser(userId);
            var revokedToken = new RefreshToken
            {
                Token = "revokedToken123",
                UserId = userId,
                User = user,
                ExpiresAt = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                IsRevoked = true,
                RevokedAt = DateTime.Now.AddMinutes(-5)
            };

            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();

            mockRequestCookies.Setup(x => x["4CLC-Auth-SRT"]).Returns("revokedToken123");
            mockRequest.Setup(x => x.Cookies).Returns(mockRequestCookies.Object);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Mock repository to return revoked token
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("revokedToken123"))
                .ReturnsAsync(revokedToken);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshToken());
            Assert.Contains("revoked", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RefreshToken_WithExpiredToken_ShouldThrowRefreshTokenException()
        {
            // Arrange - Fully mocked
            var userId = Guid.NewGuid();
            var user = AuthMockData.GetMockUser(userId);
            var expiredToken = new RefreshToken
            {
                Token = "expiredToken123",
                UserId = userId,
                User = user,
                ExpiresAt = DateTime.Now.AddDays(-1), // Expired yesterday
                CreatedAt = DateTime.Now.AddDays(-8),
                IsRevoked = false
            };

            var mockHttpContext = new Mock<HttpContext>();
            var mockRequest = new Mock<HttpRequest>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();
            var mockRequestCookies = new Mock<IRequestCookieCollection>();

            mockRequestCookies.Setup(x => x["4CLC-Auth-SRT"]).Returns("expiredToken123");
            mockRequest.Setup(x => x.Cookies).Returns(mockRequestCookies.Object);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Mock repository to return expired token
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("expiredToken123"))
                .ReturnsAsync(expiredToken);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RefreshTokenException>(() => _authService.RefreshToken());
            Assert.Contains("Expired", exception.Message);
        }

        [Fact]
        public async Task RefreshTokenMobile_WithValidToken_ShouldReturnNewTokens()
        {
            // Arrange - Fully mocked
            var userId = Guid.NewGuid();
            var user = AuthMockData.GetMockUser(userId);
            var userDto = AuthMockData.GetMockUserDto(userId, user.Username, user.Email);
            var oldRefreshToken = new RefreshToken
            {
                Token = "validMobileRefreshToken123",
                UserId = userId,
                User = user,
                ExpiresAt = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                IsRevoked = false
            };

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

            // Mock repository to return the token
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("validMobileRefreshToken123"))
                .ReturnsAsync(oldRefreshToken);

            _mockRefreshTokenRepository
                .Setup(x => x.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.RefreshTokenMobile("validMobileRefreshToken123");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.User);
            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
            Assert.True(oldRefreshToken.IsRevoked); // Verify old token was revoked
            Assert.NotNull(oldRefreshToken.RevokedAt);
            
            // Verify repository methods were called
            _mockRefreshTokenRepository.Verify(x => x.GetByTokenAsync("validMobileRefreshToken123"), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenMobile_WithInvalidToken_ShouldThrowRefreshTokenException()
        {
            // Arrange - Fully mocked
            // Mock repository to return null (token not found)
            _mockRefreshTokenRepository
                .Setup(x => x.GetByTokenAsync("invalidMobileToken123"))
                .ReturnsAsync((RefreshToken?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<RefreshTokenException>(
                () => _authService.RefreshTokenMobile("invalidMobileToken123"));
            Assert.Contains("Invalid or expired", exception.Message);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_WithValidUser_ShouldRevokeTokenAndClearCookies()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = AuthMockData.GetMockUser(userId);
            user.Status = "Online";
            
            var activeToken = new RefreshToken
            {
                Token = "activeToken123",
                UserId = userId,
                User = user,
                ExpiresAt = DateTime.Now.AddDays(7),
                CreatedAt = DateTime.Now,
                IsRevoked = false
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();

            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            _mockRefreshTokenRepository
                .Setup(x => x.GetLatestActiveTokenForUserAsync(userId))
                .ReturnsAsync(activeToken);

            _mockRefreshTokenRepository
                .Setup(x => x.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _authService.Logout();

            // Assert
            Assert.True(activeToken.IsRevoked);
            Assert.NotNull(activeToken.RevokedAt);
            Assert.Equal("Offline", user.Status);
            mockCookies.Verify(x => x.Delete("4CLC-XSRF-TOKEN"), Times.Once);
            mockCookies.Verify(x => x.Delete("4CLC-Auth-SRT"), Times.Once);
        }

        [Fact]
        public async Task Logout_WithNoActiveToken_ShouldClearCookiesOnly()
        {
            // Arrange
            var userId = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();

            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            _mockRefreshTokenRepository
                .Setup(x => x.GetLatestActiveTokenForUserAsync(userId))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            await _authService.Logout();

            // Assert
            mockCookies.Verify(x => x.Delete("4CLC-XSRF-TOKEN"), Times.Once);
            mockCookies.Verify(x => x.Delete("4CLC-Auth-SRT"), Times.Once);
        }

        [Fact]
        public async Task Logout_WithUnauthenticatedUser_ShouldClearCookiesOnly()
        {
            // Arrange
            var claims = new List<Claim>(); // No NameIdentifier claim
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockCookies = new Mock<IResponseCookies>();

            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            mockResponse.Setup(x => x.Cookies).Returns(mockCookies.Object);
            mockHttpContext.Setup(x => x.Response).Returns(mockResponse.Object);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Act
            await _authService.Logout();

            // Assert
            mockCookies.Verify(x => x.Delete("4CLC-XSRF-TOKEN"), Times.Once);
            mockCookies.Verify(x => x.Delete("4CLC-Auth-SRT"), Times.Once);
        }

        #endregion

        #region Additional ChangePassword Tests

        [Fact]
        public async Task ChangePassword_AdminChangingStaffPassword_ShouldSucceed()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var staffId = Guid.NewGuid();
            var changePasswordDto = AuthMockData.GetValidChangePasswordDto();
            var staffUser = AuthMockData.GetMockUser(staffId);
            staffUser.UserRole = UserRole.Staff;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
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
                .Setup(x => x.GetByIdAsync(staffId))
                .ReturnsAsync(staffUser);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(changePasswordDto.NewPassword))
                .Returns("newHashedPassword");

            _mockUserRepository
                .Setup(x => x.UpdateAsync(staffUser))
                .Returns(Task.CompletedTask);

            _mockRefreshTokenRepository
                .Setup(x => x.RevokeAllForUserAsync(staffId))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _authService.ChangePassword(staffId, changePasswordDto);

            // Assert
            _mockPasswordHashingService.Verify(x => x.HashPassword(changePasswordDto.NewPassword), Times.Once);
            _mockUserRepository.Verify(x => x.UpdateAsync(staffUser), Times.Once);
            _mockRefreshTokenRepository.Verify(x => x.RevokeAllForUserAsync(staffId), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_AdminChangingSuperAdminPassword_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var superAdminId = Guid.NewGuid();
            var changePasswordDto = AuthMockData.GetValidChangePasswordDto();
            var superAdminUser = AuthMockData.GetMockUser(superAdminId);
            superAdminUser.UserRole = UserRole.SuperAdmin;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, adminId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(superAdminId))
                .ReturnsAsync(superAdminUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _authService.ChangePassword(superAdminId, changePasswordDto));
            Assert.Contains("SuperAdmin", exception.Message);
        }

        [Fact]
        public async Task ChangePassword_WithNonExistentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var changePasswordDto = AuthMockData.GetValidChangePasswordDto();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(claimsPrincipal);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync((User?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _authService.ChangePassword(targetUserId, changePasswordDto));
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region Register Additional Tests

        [Fact]
        public async Task Register_WithDuplicateUsername_ShouldThrowException()
        {
            // Arrange
            var registerDto = AuthMockData.GetValidStudentRegisterDto();

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(registerDto.Username, registerDto.Email, registerDto.PhoneNumber))
                .ThrowsAsync(new ArgumentException("Username already exists"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.Register(registerDto));
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var registerDto = AuthMockData.GetValidStudentRegisterDto();

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(registerDto.Username, registerDto.Email, registerDto.PhoneNumber))
                .ThrowsAsync(new ArgumentException("Email already exists"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.Register(registerDto));
        }

        [Fact]
        public async Task Register_WithTeacherRole_ShouldCreateTeacher()
        {
            // Arrange
            var registerDto = AuthMockData.GetValidTeacherRegisterDto();
            var newTeacher = new Teacher
            {
                Id = Guid.NewGuid(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                UserRole = UserRole.Teacher
            };
            var userDto = AuthMockData.GetMockUserDto(newTeacher.Id, newTeacher.Username, newTeacher.Email);

            _mockUserValidationService
                .Setup(x => x.ValidateUniqueUserAsync(registerDto.Username, registerDto.Email, registerDto.PhoneNumber))
                .Returns(Task.FromResult(0));

            _mockMapper
                .Setup(x => x.Map(registerDto, It.IsAny<Teacher>()))
                .Callback<RegisterUserDto, Teacher>((src, dest) =>
                {
                    dest.Username = src.Username;
                    dest.Email = src.Email;
                });

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(registerDto.Password))
                .Returns("hashedPassword123");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(newTeacher);

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
        }

        #endregion
    }
}
