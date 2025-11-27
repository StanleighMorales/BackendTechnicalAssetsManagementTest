using BackendTechnicalAssetsManagement.src.Authorization;
using BackendTechnicalAssetsManagement.src.Classes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Authorization
{
    public class ViewProfileRequirementTests
    {
        private readonly ViewProfileRequirement _requirement;

        public ViewProfileRequirementTests()
        {
            _requirement = new ViewProfileRequirement();
        }

        #region Own Profile Access Tests

        [Fact]
        public async Task HandleRequirementAsync_UserViewingOwnProfile_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "Student");
            var resource = CreateUserResource(userId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_StudentViewingOwnProfile_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "Student");
            var resource = CreateUserResource(userId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_TeacherViewingOwnProfile_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateUser(userId, "Teacher");
            var resource = CreateUserResource(userId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        #endregion

        #region Admin Access to Any Profile Tests

        [Fact]
        public async Task HandleRequirementAsync_AdminViewingAnyProfile_ShouldSucceed()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var user = CreateUser(adminId, "Admin");
            var resource = CreateUserResource(targetUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_StaffViewingAnyProfile_ShouldSucceed()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var user = CreateUser(staffId, "Staff");
            var resource = CreateUserResource(targetUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_SuperAdminViewingAnyProfile_ShouldSucceed()
        {
            // Arrange
            var superAdminId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var user = CreateUser(superAdminId, "SuperAdmin");
            var resource = CreateUserResource(targetUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        #endregion

        #region Unauthorized Access Tests

        [Fact]
        public async Task HandleRequirementAsync_StudentViewingOtherProfile_ShouldNotSucceed()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var user = CreateUser(studentId, "Student");
            var resource = CreateUserResource(otherUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_TeacherViewingOtherProfile_ShouldNotSucceed()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var user = CreateUser(teacherId, "Teacher");
            var resource = CreateUserResource(otherUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithInvalidUserId_ShouldFail()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid-guid"),
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var resource = CreateUserResource(Guid.NewGuid());
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithMissingUserId_ShouldFail()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Student")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var resource = CreateUserResource(Guid.NewGuid());
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task HandleRequirementAsync_WithNoRole_ShouldNotSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            
            var resource = CreateUserResource(otherUserId);
            var handler = new ViewProfileRequirement.ViewProfileHandler();
            var context = new AuthorizationHandlerContext(new[] { _requirement }, user, resource);

            // Act
            await handler.HandleAsync(context);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        #endregion

        #region Helper Methods

        private ClaimsPrincipal CreateUser(Guid userId, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        private User CreateUserResource(Guid userId)
        {
            return new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };
        }

        #endregion
    }
}
