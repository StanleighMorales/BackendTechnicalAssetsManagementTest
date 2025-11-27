using BackendTechnicalAssetsManagement.src.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Authorization
{
    public class SuperAdminBypassHandlerTests
    {
        private readonly SuperAdminBypassHandler _handler;

        public SuperAdminBypassHandlerTests()
        {
            _handler = new SuperAdminBypassHandler();
        }

        #region SuperAdmin Bypass Tests

        [Fact]
        public async Task HandleAsync_WithSuperAdminRole_ShouldSucceedAllRequirements()
        {
            // Arrange
            var user = CreateUser("SuperAdmin");
            var requirements = new List<IAuthorizationRequirement>
            {
                new TestRequirement(),
                new TestRequirement(),
                new TestRequirement()
            };
            var context = new AuthorizationHandlerContext(requirements, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.Empty(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithSuperAdminRole_ShouldSucceedSingleRequirement()
        {
            // Arrange
            var user = CreateUser("SuperAdmin");
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleAsync_WithSuperAdminRole_ShouldSucceedMultipleRequirements()
        {
            // Arrange
            var user = CreateUser("SuperAdmin");
            var requirements = new IAuthorizationRequirement[]
            {
                new TestRequirement(),
                new TestRequirement(),
                new TestRequirement(),
                new TestRequirement(),
                new TestRequirement()
            };
            var context = new AuthorizationHandlerContext(requirements, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
            Assert.Empty(context.PendingRequirements);
        }

        #endregion

        #region Non-SuperAdmin Handling Tests

        [Fact]
        public async Task HandleAsync_WithAdminRole_ShouldNotSucceed()
        {
            // Arrange
            var user = CreateUser("Admin");
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithStaffRole_ShouldNotSucceed()
        {
            // Arrange
            var user = CreateUser("Staff");
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithStudentRole_ShouldNotSucceed()
        {
            // Arrange
            var user = CreateUser("Student");
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithTeacherRole_ShouldNotSucceed()
        {
            // Arrange
            var user = CreateUser("Teacher");
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithNoRole_ShouldNotSucceed()
        {
            // Arrange
            var user = CreateUser(null);
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.Single(context.PendingRequirements);
        }

        [Fact]
        public async Task HandleAsync_WithMultipleNonSuperAdminRoles_ShouldNotSucceed()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Role, "Staff")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var requirement = new TestRequirement();
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);

            // Act
            await _handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        #endregion

        #region Helper Methods

        private ClaimsPrincipal CreateUser(string? role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private class TestRequirement : IAuthorizationRequirement { }

        #endregion
    }
}
