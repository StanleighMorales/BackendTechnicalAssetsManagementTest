using BackendTechnicalAssetsManagement.src.Services;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class PasswordHashingServiceTests
    {
        private readonly PasswordHashingService _passwordHashingService;

        public PasswordHashingServiceTests()
        {
            _passwordHashingService = new PasswordHashingService();
        }

        #region HashPassword Tests

        [Fact]
        public void HashPassword_WithValidPassword_ShouldReturnHashedPassword()
        {
            // Arrange
            var password = "ValidPassword123!";

            // Act
            var hashedPassword = _passwordHashingService.HashPassword(password);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.NotEqual(password, hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        [Fact]
        public void HashPassword_WithSamePassword_ShouldReturnDifferentHashes()
        {
            // Arrange
            var password = "SamePassword123!";

            // Act
            var hash1 = _passwordHashingService.HashPassword(password);
            var hash2 = _passwordHashingService.HashPassword(password);

            // Assert
            Assert.NotNull(hash1);
            Assert.NotNull(hash2);
            Assert.NotEqual(hash1, hash2); // BCrypt generates different salts
        }

        [Fact]
        public void HashPassword_WithEmptyPassword_ShouldReturnHash()
        {
            // Arrange
            var emptyPassword = string.Empty;

            // Act
            var hashedPassword = _passwordHashingService.HashPassword(emptyPassword);

            // Assert - BCrypt allows empty passwords and hashes them
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.StartsWith("$2", hashedPassword);
        }

        [Fact]
        public void HashPassword_WithNullPassword_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? nullPassword = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _passwordHashingService.HashPassword(nullPassword!));
        }

        [Theory]
        [InlineData("Short1!")]
        [InlineData("VeryLongPasswordThatExceedsNormalLimits123!@#$%^&*()")]
        [InlineData("P@ssw0rd")]
        [InlineData("ComplexP@ssw0rd!2024")]
        public void HashPassword_WithVariousValidPasswords_ShouldReturnValidHashes(string password)
        {
            // Act
            var hashedPassword = _passwordHashingService.HashPassword(password);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.NotEqual(password, hashedPassword);
            Assert.StartsWith("$2", hashedPassword); // BCrypt hash format
        }

        #endregion

        #region VerifyPassword Tests

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "CorrectPassword123!";
            var hashedPassword = _passwordHashingService.HashPassword(password);

            // Act
            var result = _passwordHashingService.VerifyPassword(password, hashedPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var correctPassword = "CorrectPassword123!";
            var incorrectPassword = "WrongPassword456!";
            var hashedPassword = _passwordHashingService.HashPassword(correctPassword);

            // Act
            var result = _passwordHashingService.VerifyPassword(incorrectPassword, hashedPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_WithCaseSensitivePassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "CaseSensitive123!";
            var wrongCasePassword = "casesensitive123!";
            var hashedPassword = _passwordHashingService.HashPassword(password);

            // Act
            var result = _passwordHashingService.VerifyPassword(wrongCasePassword, hashedPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_WithInvalidHash_ShouldThrowException()
        {
            // Arrange
            var password = "ValidPassword123!";
            var invalidHash = "not-a-valid-bcrypt-hash";

            // Act & Assert
            Assert.Throws<BCrypt.Net.SaltParseException>(() => 
                _passwordHashingService.VerifyPassword(password, invalidHash));
        }

        [Fact]
        public void VerifyPassword_WithEmptyHash_ShouldThrowException()
        {
            // Arrange
            var password = "ValidPassword123!";
            var emptyHash = string.Empty;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _passwordHashingService.VerifyPassword(password, emptyHash));
        }

        [Fact]
        public void VerifyPassword_WithNullPassword_ShouldThrowArgumentNullException()
        {
            // Arrange
            string? nullPassword = null;
            var validHash = _passwordHashingService.HashPassword("ValidPassword123!");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _passwordHashingService.VerifyPassword(nullPassword!, validHash));
        }

        [Fact]
        public void VerifyPassword_WithNullHash_ShouldThrowArgumentNullException()
        {
            // Arrange
            var password = "ValidPassword123!";
            string? nullHash = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _passwordHashingService.VerifyPassword(password, nullHash!));
        }

        [Theory]
        [InlineData("Password123!", "Password123!")]
        [InlineData("ComplexP@ss123", "ComplexP@ss123")]
        [InlineData("Simple1!", "Simple1!")]
        public void VerifyPassword_WithMatchingPasswords_ShouldReturnTrue(string original, string verify)
        {
            // Arrange
            var hashedPassword = _passwordHashingService.HashPassword(original);

            // Act
            var result = _passwordHashingService.VerifyPassword(verify, hashedPassword);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("Password123!", "password123!")]
        [InlineData("ComplexP@ss123", "ComplexP@ss124")]
        [InlineData("Simple1!", "Simple1")]
        public void VerifyPassword_WithNonMatchingPasswords_ShouldReturnFalse(string original, string verify)
        {
            // Arrange
            var hashedPassword = _passwordHashingService.HashPassword(original);

            // Act
            var result = _passwordHashingService.VerifyPassword(verify, hashedPassword);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void HashAndVerify_CompleteWorkflow_ShouldWorkCorrectly()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act - Hash the password
            var hashedPassword = _passwordHashingService.HashPassword(password);
            
            // Act - Verify correct password
            var correctVerification = _passwordHashingService.VerifyPassword(password, hashedPassword);
            
            // Act - Verify incorrect password
            var incorrectVerification = _passwordHashingService.VerifyPassword("WrongPassword!", hashedPassword);

            // Assert
            Assert.NotNull(hashedPassword);
            Assert.NotEmpty(hashedPassword);
            Assert.True(correctVerification);
            Assert.False(incorrectVerification);
        }

        [Fact]
        public void HashPassword_MultipleHashesOfSamePassword_ShouldAllVerifyCorrectly()
        {
            // Arrange
            var password = "ConsistentPassword123!";

            // Act - Create multiple hashes
            var hash1 = _passwordHashingService.HashPassword(password);
            var hash2 = _passwordHashingService.HashPassword(password);
            var hash3 = _passwordHashingService.HashPassword(password);

            // Act - Verify all hashes
            var verify1 = _passwordHashingService.VerifyPassword(password, hash1);
            var verify2 = _passwordHashingService.VerifyPassword(password, hash2);
            var verify3 = _passwordHashingService.VerifyPassword(password, hash3);

            // Assert
            Assert.NotEqual(hash1, hash2);
            Assert.NotEqual(hash2, hash3);
            Assert.NotEqual(hash1, hash3);
            Assert.True(verify1);
            Assert.True(verify2);
            Assert.True(verify3);
        }

        #endregion
    }
}
