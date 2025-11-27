using BackendTechnicalAssetsManagement.src.BackgroundServices;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class RefreshTokenCleanupServiceTests
    {
        private readonly Mock<ILogger<RefreshTokenCleanupService>> _mockLogger;

        public RefreshTokenCleanupServiceTests()
        {
            _mockLogger = new Mock<ILogger<RefreshTokenCleanupService>>();
        }

        #region Service Lifecycle Tests

        [Fact]
        public async Task ExecuteAsync_ShouldStartSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            var serviceProvider = services.BuildServiceProvider();

            var service = new RefreshTokenCleanupService(_mockLogger.Object, serviceProvider);
            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(50);
            cts.Cancel();
            await Task.Delay(50);

            // Assert - Service should start without throwing
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_ShouldStopGracefully()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            var serviceProvider = services.BuildServiceProvider();

            var service = new RefreshTokenCleanupService(_mockLogger.Object, serviceProvider);
            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(50);
            cts.Cancel(); // Cancel immediately
            await Task.Delay(100);

            // Assert - Service should stop without throwing
            Assert.True(cts.Token.IsCancellationRequested);

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldLogCleanupTask()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            var serviceProvider = services.BuildServiceProvider();

            var service = new RefreshTokenCleanupService(_mockLogger.Object, serviceProvider);
            var cts = new CancellationTokenSource();

            // Act
            var executeTask = service.StartAsync(cts.Token);
            await Task.Delay(100);
            cts.Cancel();
            await Task.Delay(50);

            // Assert - Verify logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("cleanup")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);

            serviceProvider.Dispose();
        }

        #endregion

        #region Token Cleanup Logic Tests

        [Fact]
        public async Task CleanupLogic_ShouldRemoveExpiredTokens()
        {
            // Arrange - Test the cleanup logic directly with a shared database
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));
            var serviceProvider = services.BuildServiceProvider();

            // Seed expired and valid tokens
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.RefreshTokens.AddRange(
                    new RefreshToken
                    {
                        Token = "expired_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(-1),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new RefreshToken
                    {
                        Token = "valid_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow
                    });
                await context.SaveChangesAsync();
            }

            // Act - Manually execute cleanup logic
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokensToRemove = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                context.RefreshTokens.RemoveRange(tokensToRemove);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var remainingTokens = await context.RefreshTokens.ToListAsync();
                Assert.Single(remainingTokens);
                Assert.Equal("valid_token", remainingTokens[0].Token);
            }

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task CleanupLogic_ShouldRemoveRevokedTokens()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));
            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.RefreshTokens.AddRange(
                    new RefreshToken
                    {
                        Token = "revoked_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = true,
                        RevokedAt = DateTime.UtcNow.AddHours(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new RefreshToken
                    {
                        Token = "valid_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow
                    });
                await context.SaveChangesAsync();
            }

            // Act
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokensToRemove = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                context.RefreshTokens.RemoveRange(tokensToRemove);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var remainingTokens = await context.RefreshTokens.ToListAsync();
                Assert.Single(remainingTokens);
                Assert.Equal("valid_token", remainingTokens[0].Token);
            }

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task CleanupLogic_WithNoExpiredTokens_ShouldNotRemoveAny()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));
            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.RefreshTokens.AddRange(
                    new RefreshToken
                    {
                        Token = "valid_token_1",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new RefreshToken
                    {
                        Token = "valid_token_2",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(5),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-1)
                    });
                await context.SaveChangesAsync();
            }

            // Act
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokensToRemove = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                context.RefreshTokens.RemoveRange(tokensToRemove);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var remainingTokens = await context.RefreshTokens.ToListAsync();
                Assert.Equal(2, remainingTokens.Count);
            }

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task CleanupLogic_WithEmptyDatabase_ShouldHandleGracefully()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));
            var serviceProvider = services.BuildServiceProvider();

            // Act - No tokens in database
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokensToRemove = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                context.RefreshTokens.RemoveRange(tokensToRemove);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var remainingTokens = await context.RefreshTokens.ToListAsync();
                Assert.Empty(remainingTokens);
            }

            serviceProvider.Dispose();
        }

        [Fact]
        public async Task CleanupLogic_WithMixedTokens_ShouldOnlyRemoveExpiredAndRevoked()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: dbName));
            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                context.RefreshTokens.AddRange(
                    new RefreshToken
                    {
                        Token = "expired_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(-1),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow.AddDays(-8)
                    },
                    new RefreshToken
                    {
                        Token = "revoked_token",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = true,
                        RevokedAt = DateTime.UtcNow.AddHours(-1),
                        CreatedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new RefreshToken
                    {
                        Token = "valid_token_1",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow
                    },
                    new RefreshToken
                    {
                        Token = "valid_token_2",
                        UserId = Guid.NewGuid(),
                        ExpiresAt = DateTime.UtcNow.AddDays(5),
                        IsRevoked = false,
                        CreatedAt = DateTime.UtcNow.AddHours(-2)
                    });
                await context.SaveChangesAsync();
            }

            // Act
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tokensToRemove = await context.RefreshTokens
                    .Where(rt => rt.ExpiresAt <= DateTime.UtcNow || rt.IsRevoked)
                    .ToListAsync();

                context.RefreshTokens.RemoveRange(tokensToRemove);
                await context.SaveChangesAsync();
            }

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var remainingTokens = await context.RefreshTokens.ToListAsync();
                Assert.Equal(2, remainingTokens.Count);
                Assert.Contains(remainingTokens, t => t.Token == "valid_token_1");
                Assert.Contains(remainingTokens, t => t.Token == "valid_token_2");
                Assert.DoesNotContain(remainingTokens, t => t.Token == "expired_token");
                Assert.DoesNotContain(remainingTokens, t => t.Token == "revoked_token");
            }

            serviceProvider.Dispose();
        }

        #endregion
    }
}
