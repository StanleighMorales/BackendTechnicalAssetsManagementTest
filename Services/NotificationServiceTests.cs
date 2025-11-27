using BackendTechnicalAssetsManagement.src.Hubs;
using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class NotificationServiceTests
    {
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly Mock<IHubClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly NotificationService _notificationService;

        public NotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            _mockLogger = new Mock<ILogger<NotificationService>>();
            _mockClients = new Mock<IHubClients>();
            _mockClientProxy = new Mock<IClientProxy>();

            _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
            _notificationService = new NotificationService(_mockHubContext.Object, _mockLogger.Object);
        }

        #region SendNewPendingRequestNotificationAsync Tests

        [Fact]
        public async Task SendNewPendingRequestNotificationAsync_WithValidData_ShouldSendToAdminStaffGroup()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var itemName = "Laptop";
            var borrowerName = "John Doe";
            var reservedFor = DateTime.Now.AddDays(1);

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendNewPendingRequestNotificationAsync(
                lentItemId, itemName, borrowerName, reservedFor);

            // Assert
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveNewPendingRequest",
                    It.Is<object[]>(o => o.Length == 1),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendNewPendingRequestNotificationAsync_WithNullReservedFor_ShouldStillSend()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var itemName = "Projector";
            var borrowerName = "Jane Smith";
            DateTime? reservedFor = null;

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendNewPendingRequestNotificationAsync(
                lentItemId, itemName, borrowerName, reservedFor);

            // Assert
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveNewPendingRequest",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendNewPendingRequestNotificationAsync_WithEmptyStrings_ShouldStillSend()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var itemName = "";
            var borrowerName = "";
            DateTime? reservedFor = null;

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendNewPendingRequestNotificationAsync(
                lentItemId, itemName, borrowerName, reservedFor);

            // Assert
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
        }

        [Fact]
        public async Task SendNewPendingRequestNotificationAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var itemName = "Camera";
            var borrowerName = "Bob Johnson";
            var reservedFor = DateTime.Now.AddDays(2);

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Throws(new Exception("SignalR connection error"));

            // Act
            await _notificationService.SendNewPendingRequestNotificationAsync(
                lentItemId, itemName, borrowerName, reservedFor);

            // Assert - Should log error but not throw
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SendApprovalNotificationAsync Tests

        [Fact]
        public async Task SendApprovalNotificationAsync_WithUserId_ShouldSendToUserAndAdminStaff()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var itemName = "Tablet";
            var borrowerName = "Alice Brown";

            _mockClients
                .Setup(x => x.Group($"user_{userId}"))
                .Returns(_mockClientProxy.Object);

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendApprovalNotificationAsync(
                lentItemId, userId, itemName, borrowerName);

            // Assert
            _mockClients.Verify(x => x.Group($"user_{userId}"), Times.Once);
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveApprovalNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Exactly(2)); // Once for user, once for admin_staff
        }

        [Fact]
        public async Task SendApprovalNotificationAsync_WithoutUserId_ShouldOnlySendToAdminStaff()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            Guid? userId = null;
            var itemName = "Monitor";
            var borrowerName = "Charlie Wilson";

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendApprovalNotificationAsync(
                lentItemId, userId, itemName, borrowerName);

            // Assert
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveApprovalNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once); // Only to admin_staff
        }

        [Fact]
        public async Task SendApprovalNotificationAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var itemName = "Keyboard";
            var borrowerName = "David Lee";

            _mockClients
                .Setup(x => x.Group(It.IsAny<string>()))
                .Throws(new Exception("SignalR error"));

            // Act
            await _notificationService.SendApprovalNotificationAsync(
                lentItemId, userId, itemName, borrowerName);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send approval")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SendStatusChangeNotificationAsync Tests

        [Fact]
        public async Task SendStatusChangeNotificationAsync_WithUserId_ShouldSendToUserAndAdminStaff()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var itemName = "Mouse";
            var oldStatus = "Pending";
            var newStatus = "Approved";

            _mockClients
                .Setup(x => x.Group($"user_{userId}"))
                .Returns(_mockClientProxy.Object);

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendStatusChangeNotificationAsync(
                lentItemId, userId, itemName, oldStatus, newStatus);

            // Assert
            _mockClients.Verify(x => x.Group($"user_{userId}"), Times.Once);
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveStatusChangeNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Exactly(2));
        }

        [Fact]
        public async Task SendStatusChangeNotificationAsync_WithoutUserId_ShouldOnlySendToAdminStaff()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            Guid? userId = null;
            var itemName = "Headphones";
            var oldStatus = "Approved";
            var newStatus = "Borrowed";

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendStatusChangeNotificationAsync(
                lentItemId, userId, itemName, oldStatus, newStatus);

            // Assert
            _mockClients.Verify(x => x.Group("admin_staff"), Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveStatusChangeNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendStatusChangeNotificationAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var itemName = "Cable";
            var oldStatus = "Borrowed";
            var newStatus = "Returned";

            _mockClients
                .Setup(x => x.Group(It.IsAny<string>()))
                .Throws(new Exception("Connection lost"));

            // Act
            await _notificationService.SendStatusChangeNotificationAsync(
                lentItemId, userId, itemName, oldStatus, newStatus);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send status change")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region SendBroadcastNotificationAsync Tests

        [Fact]
        public async Task SendBroadcastNotificationAsync_WithMessageAndData_ShouldSendToAllClients()
        {
            // Arrange
            var message = "System maintenance scheduled";
            var data = new { MaintenanceTime = DateTime.Now.AddHours(2), Duration = "30 minutes" };

            _mockClients
                .Setup(x => x.All)
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendBroadcastNotificationAsync(message, data);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveBroadcastNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendBroadcastNotificationAsync_WithMessageOnly_ShouldSendToAllClients()
        {
            // Arrange
            var message = "New feature available!";

            _mockClients
                .Setup(x => x.All)
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendBroadcastNotificationAsync(message);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    "ReceiveBroadcastNotification",
                    It.IsAny<object[]>(),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task SendBroadcastNotificationAsync_WithNullData_ShouldStillSend()
        {
            // Arrange
            var message = "Important announcement";
            object? data = null;

            _mockClients
                .Setup(x => x.All)
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendBroadcastNotificationAsync(message, data);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
        }

        [Fact]
        public async Task SendBroadcastNotificationAsync_WhenExceptionOccurs_ShouldLogError()
        {
            // Arrange
            var message = "Test broadcast";

            _mockClients
                .Setup(x => x.All)
                .Throws(new Exception("Broadcast failed"));

            // Act
            await _notificationService.SendBroadcastNotificationAsync(message);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send broadcast")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Theory]
        [InlineData("Message 1")]
        [InlineData("Message 2")]
        [InlineData("Message 3")]
        public async Task SendBroadcastNotificationAsync_WithVariousMessages_ShouldSendToAll(string message)
        {
            // Arrange
            _mockClients
                .Setup(x => x.All)
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendBroadcastNotificationAsync(message);

            // Assert
            _mockClients.Verify(x => x.All, Times.Once);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task NotificationService_ShouldLogInformationOnSuccessfulSend()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var itemName = "Test Item";
            var borrowerName = "Test User";

            _mockClients
                .Setup(x => x.Group("admin_staff"))
                .Returns(_mockClientProxy.Object);

            // Act
            await _notificationService.SendNewPendingRequestNotificationAsync(
                lentItemId, itemName, borrowerName, null);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("notification sent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
