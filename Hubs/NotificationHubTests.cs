using BackendTechnicalAssetsManagement.src.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendTechnicalAssetsManagementTest.Hubs
{
    public class NotificationHubTests
    {
        private readonly Mock<ILogger<NotificationHub>> _mockLogger;
        private readonly Mock<HubCallerContext> _mockContext;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly NotificationHub _notificationHub;

        public NotificationHubTests()
        {
            _mockLogger = new Mock<ILogger<NotificationHub>>();
            _mockContext = new Mock<HubCallerContext>();
            _mockGroups = new Mock<IGroupManager>();

            _notificationHub = new NotificationHub(_mockLogger.Object)
            {
                Context = _mockContext.Object,
                Groups = _mockGroups.Object
            };
        }

        #region OnConnectedAsync Tests

        [Fact]
        public async Task OnConnectedAsync_ShouldLogConnectionAndCallBase()
        {
            // Arrange
            var connectionId = "test-connection-123";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client connected") && v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnConnectedAsync_WithDifferentConnectionIds_ShouldLogCorrectId()
        {
            // Arrange
            var connectionId = "unique-connection-456";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.OnConnectedAsync();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region OnDisconnectedAsync Tests

        [Fact]
        public async Task OnDisconnectedAsync_WithoutException_ShouldLogDisconnection()
        {
            // Arrange
            var connectionId = "test-connection-789";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.OnDisconnectedAsync(null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected") && v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnDisconnectedAsync_WithException_ShouldStillLogDisconnection()
        {
            // Arrange
            var connectionId = "test-connection-error";
            var exception = new Exception("Connection lost");
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.OnDisconnectedAsync(exception);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Client disconnected") && v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region JoinUserGroup Tests

        [Fact]
        public async Task JoinUserGroup_WithValidUserId_ShouldAddToGroup()
        {
            // Arrange
            var userId = "user-123";
            var connectionId = "connection-abc";
            var expectedGroupName = $"user_{userId}";

            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
            _mockGroups
                .Setup(x => x.AddToGroupAsync(connectionId, expectedGroupName, default))
                .Returns(Task.CompletedTask);

            // Act
            await _notificationHub.JoinUserGroup(userId);

            // Assert
            _mockGroups.Verify(
                x => x.AddToGroupAsync(connectionId, expectedGroupName, default),
                Times.Once,
                "Should add connection to user-specific group");
        }

        [Fact]
        public async Task JoinUserGroup_ShouldLogGroupJoin()
        {
            // Arrange
            var userId = "user-456";
            var connectionId = "connection-xyz";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.JoinUserGroup(userId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("joined group") && v.ToString()!.Contains(userId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region LeaveUserGroup Tests

        [Fact]
        public async Task LeaveUserGroup_WithValidUserId_ShouldRemoveFromGroup()
        {
            // Arrange
            var userId = "user-789";
            var connectionId = "connection-def";
            var expectedGroupName = $"user_{userId}";

            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
            _mockGroups
                .Setup(x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default))
                .Returns(Task.CompletedTask);

            // Act
            await _notificationHub.LeaveUserGroup(userId);

            // Assert
            _mockGroups.Verify(
                x => x.RemoveFromGroupAsync(connectionId, expectedGroupName, default),
                Times.Once,
                "Should remove connection from user-specific group");
        }

        [Fact]
        public async Task LeaveUserGroup_ShouldLogGroupLeave()
        {
            // Arrange
            var userId = "user-999";
            var connectionId = "connection-ghi";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.LeaveUserGroup(userId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("left group") && v.ToString()!.Contains(userId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region JoinAdminStaffGroup Tests

        [Fact]
        public async Task JoinAdminStaffGroup_ShouldAddToAdminStaffGroup()
        {
            // Arrange
            var connectionId = "admin-connection-123";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
            _mockGroups
                .Setup(x => x.AddToGroupAsync(connectionId, "admin_staff", default))
                .Returns(Task.CompletedTask);

            // Act
            await _notificationHub.JoinAdminStaffGroup();

            // Assert
            _mockGroups.Verify(
                x => x.AddToGroupAsync(connectionId, "admin_staff", default),
                Times.Once,
                "Should add connection to admin_staff group");
        }

        [Fact]
        public async Task JoinAdminStaffGroup_ShouldLogGroupJoin()
        {
            // Arrange
            var connectionId = "admin-connection-456";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.JoinAdminStaffGroup();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("joined admin_staff group") && v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region LeaveAdminStaffGroup Tests

        [Fact]
        public async Task LeaveAdminStaffGroup_ShouldRemoveFromAdminStaffGroup()
        {
            // Arrange
            var connectionId = "admin-connection-789";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
            _mockGroups
                .Setup(x => x.RemoveFromGroupAsync(connectionId, "admin_staff", default))
                .Returns(Task.CompletedTask);

            // Act
            await _notificationHub.LeaveAdminStaffGroup();

            // Assert
            _mockGroups.Verify(
                x => x.RemoveFromGroupAsync(connectionId, "admin_staff", default),
                Times.Once,
                "Should remove connection from admin_staff group");
        }

        [Fact]
        public async Task LeaveAdminStaffGroup_ShouldLogGroupLeave()
        {
            // Arrange
            var connectionId = "admin-connection-999";
            _mockContext.Setup(x => x.ConnectionId).Returns(connectionId);

            // Act
            await _notificationHub.LeaveAdminStaffGroup();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("left admin_staff group") && v.ToString()!.Contains(connectionId)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
