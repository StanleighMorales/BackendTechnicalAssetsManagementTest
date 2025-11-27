using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class RefreshTokenRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly RefreshTokenRepository _repository;

        public RefreshTokenRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _repository = new RefreshTokenRepository(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetByTokenAsync Tests

        [Fact]
        public async Task GetByTokenAsync_WithValidToken_ShouldReturnRefreshToken()
        {
            // Arrange
            var tokenString = "valid-token-123";
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var refreshToken = new RefreshToken
            {
                Token = tokenString,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByTokenAsync(tokenString);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenString, result.Token);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task GetByTokenAsync_WithInvalidToken_ShouldReturnNull()
        {
            // Arrange
            var nonExistentToken = "nonexistent-token";

            // Act
            var result = await _repository.GetByTokenAsync(nonExistentToken);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetLatestActiveTokenForUserAsync Tests

        [Fact]
        public async Task GetLatestActiveTokenForUserAsync_WithActiveTokens_ShouldReturnLatest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var olderToken = new RefreshToken
            {
                Token = "older-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                IsRevoked = false
            };

            var newerToken = new RefreshToken
            {
                Token = "newer-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddRangeAsync(new[] { olderToken, newerToken });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestActiveTokenForUserAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("newer-token", result.Token);
        }

        [Fact]
        public async Task GetLatestActiveTokenForUserAsync_WithRevokedTokens_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var revokedToken = new RefreshToken
            {
                Token = "revoked-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = true
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(revokedToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestActiveTokenForUserAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLatestActiveTokenForUserAsync_WithExpiredTokens_ShouldReturnToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var expiredToken = new RefreshToken
            {
                Token = "expired-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(expiredToken);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLatestActiveTokenForUserAsync(userId);

            // Assert - Repository doesn't filter by expiration, only by IsRevoked
            Assert.NotNull(result);
            Assert.Equal("expired-token", result.Token);
        }

        [Fact]
        public async Task GetLatestActiveTokenForUserAsync_WithNoTokens_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _repository.GetLatestActiveTokenForUserAsync(userId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidToken_ShouldAddToContext()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var refreshToken = new RefreshToken
            {
                Token = "new-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "new-token");
            Assert.NotNull(savedToken);
            Assert.Equal(userId, savedToken.UserId);
        }

        #endregion

        #region RevokeAllForUserAsync Tests

        [Fact]
        public async Task RevokeAllForUserAsync_WithMultipleTokens_ShouldRevokeAll()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var token1 = new RefreshToken
            {
                Token = "token-1",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            var token2 = new RefreshToken
            {
                Token = "token-2",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddRangeAsync(new[] { token1, token2 });
            await _context.SaveChangesAsync();

            // Act
            await _repository.RevokeAllForUserAsync(userId);
            await _context.SaveChangesAsync();

            // Assert
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            Assert.All(tokens, t => Assert.True(t.IsRevoked));
        }

        [Fact]
        public async Task RevokeAllForUserAsync_WithNoTokens_ShouldNotThrow()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.RevokeAllForUserAsync(userId);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldSaveSuccessfully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var refreshToken = new RefreshToken
            {
                Token = "save-test-token",
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(refreshToken);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == "save-test-token");
            Assert.NotNull(savedToken);
        }

        #endregion
    }
}
