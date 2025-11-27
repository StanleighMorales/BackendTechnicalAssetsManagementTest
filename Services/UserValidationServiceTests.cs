using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Moq;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class UserValidationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly UserValidationService _userValidationService;

        public UserValidationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _userValidationService = new UserValidationService(_mockUserRepository.Object);
        }

        #region ValidateUniqueUserAsync Tests

        [Fact]
        public async Task ValidateUniqueUserAsync_WithUniqueCredentials_ShouldNotThrowException()
        {
            // Arrange
            var username = "newuser123";
            var email = "newuser@test.com";
            var phoneNumber = "1234567890";

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber);

            // Verify all checks were performed
            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Fact]
        public async Task ValidateUniqueUserAsync_WithDuplicateUsername_ShouldThrowException()
        {
            // Arrange
            var username = "existinguser";
            var email = "newemail@test.com";
            var phoneNumber = "1234567890";
            var existingUser = UserMockData.GetMockStudent();
            existingUser.Username = username;

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber));

            Assert.Contains("Username", exception.Message);
            Assert.Contains("already taken", exception.Message);
            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateUniqueUserAsync_WithDuplicateEmail_ShouldThrowException()
        {
            // Arrange
            var username = "newuser";
            var email = "existing@test.com";
            var phoneNumber = "1234567890";
            var existingUser = UserMockData.GetMockStudent();
            existingUser.Email = email;

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber));

            Assert.Contains("Email", exception.Message);
            Assert.Contains("already exist", exception.Message);
            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateUniqueUserAsync_WithDuplicatePhoneNumber_ShouldThrowException()
        {
            // Arrange
            var username = "newuser";
            var email = "newemail@test.com";
            var phoneNumber = "1234567890";
            var existingUser = UserMockData.GetMockStudent();
            existingUser.PhoneNumber = phoneNumber;

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber));

            Assert.Contains("Phone Number", exception.Message);
            Assert.Contains("already used", exception.Message);
            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Theory]
        [InlineData("user1", "email1@test.com", "1111111111")]
        [InlineData("user2", "email2@test.com", "2222222222")]
        [InlineData("user3", "email3@test.com", "3333333333")]
        public async Task ValidateUniqueUserAsync_WithVariousUniqueCredentials_ShouldNotThrowException(
            string username, string email, string phoneNumber)
        {
            // Arrange
            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByPhoneNumberAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber);

            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        [Fact]
        public async Task ValidateUniqueUserAsync_WithEmptyStrings_ShouldStillValidate()
        {
            // Arrange
            var username = "";
            var email = "";
            var phoneNumber = "";

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(username))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByEmailAsync(email))
                .ReturnsAsync((User?)null);

            _mockUserRepository
                .Setup(x => x.GetByPhoneNumberAsync(phoneNumber))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await _userValidationService.ValidateUniqueUserAsync(username, email, phoneNumber);

            _mockUserRepository.Verify(x => x.GetByUsernameAsync(username), Times.Once);
            _mockUserRepository.Verify(x => x.GetByEmailAsync(email), Times.Once);
            _mockUserRepository.Verify(x => x.GetByPhoneNumberAsync(phoneNumber), Times.Once);
        }

        #endregion
    }
}
