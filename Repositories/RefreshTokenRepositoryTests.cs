using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
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
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var tokenString = "valid-refresh-token-123";
            var refreshToken = new RefreshToken
            {
                Token = tokenString,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
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
            Assert.Equal(user.Id, result.UserId);
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
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = false
            };

            var newerToken = new RefreshToken
            {
                Token = "newer-token",
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ExpiresAt = DateTime.UtcNow.AddDays(6),
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
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = true,
                RevokedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.AddAsync(refreshToken);
            await _repository.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
            Assert.NotNull(savedToken);
            Assert.Equal("new-token", savedToken.Token);
        }

        #endregion

        #region RevokeAllForUserAsync Tests

        [Fact]
        public async Task RevokeAllForUserAsync_WithActiveTokens_ShouldRevokeAll()
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
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            var token2 = new RefreshToken
            {
                Token = "token-2",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddRangeAsync(new[] { token1, token2 });
            await _context.SaveChangesAsync();

            // Act
            await _repository.RevokeAllForUserAsync(userId);
            await _repository.SaveChangesAsync();

            // Assert
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            Assert.All(tokens, t => Assert.True(t.IsRevoked));
            Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        }

        [Fact]
        public async Task RevokeAllForUserAsync_WithNoTokens_ShouldNotThrow()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.RevokeAllForUserAsync(userId);
            await _repository.SaveChangesAsync();
        }

        [Fact]
        public async Task RevokeAllForUserAsync_WithAlreadyRevokedTokens_ShouldNotAffectThem()
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

            var originalRevokedAt = DateTime.UtcNow.AddDays(-1);
            var revokedToken = new RefreshToken
            {
                Token = "already-revoked",
                UserId = userId,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiresAt = DateTime.UtcNow.AddDays(5),
                IsRevoked = true,
                RevokedAt = originalRevokedAt
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(revokedToken);
            await _context.SaveChangesAsync();

            // Act
            await _repository.RevokeAllForUserAsync(userId);
            await _repository.SaveChangesAsync();

            // Assert
            var token = await _context.RefreshTokens.FindAsync(revokedToken.Id);
            Assert.NotNull(token);
            Assert.True(token.IsRevoked);
            Assert.Equal(originalRevokedAt, token.RevokedAt); // Should not change
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_ShouldPersistChanges()
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
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false
            };

            await _context.Users.AddAsync(user);
            await _context.RefreshTokens.AddAsync(refreshToken);

            // Act
            await _repository.SaveChangesAsync();

            // Assert
            var savedToken = await _context.RefreshTokens.FindAsync(refreshToken.Id);
            Assert.NotNull(savedToken);
        }

        #endregion
    }
}
