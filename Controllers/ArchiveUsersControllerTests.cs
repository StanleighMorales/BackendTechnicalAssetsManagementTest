using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Users;
using BackendTechnicalAssetsManagement.src.IService;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class ArchiveUsersControllerTests
    {
        private readonly Mock<IArchiveUserService> _mockArchiveService;
        private readonly ArchiveUsersController _controller;

        public ArchiveUsersControllerTests()
        {
            _mockArchiveService = new Mock<IArchiveUserService>();
            _controller = new ArchiveUsersController(_mockArchiveService.Object);
        }

        #region GetAll Tests

        [Fact]
        public async Task GetAllArchivedUsers_WithArchivedUsers_ShouldReturnOkWithList()
        {
            // Arrange
            var archivedUsers = new List<ArchiveUserDto>
            {
                new ArchiveUserDto
                {
                    Id = Guid.NewGuid(),
                    Username = "archived.user1",
                    Email = "user1@test.com",
                    FirstName = "John",
                    LastName = "Doe",
                    ArchivedAt = DateTime.UtcNow
                },
                new ArchiveUserDto
                {
                    Id = Guid.NewGuid(),
                    Username = "archived.user2",
                    Email = "user2@test.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    ArchivedAt = DateTime.UtcNow
                }
            };

            _mockArchiveService
                .Setup(x => x.GetAllArchivedUsersAsync())
                .ReturnsAsync(archivedUsers);

            // Act
            var result = await _controller.GetAllArchivedUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetAllArchivedUsersAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetAllArchivedUsers_WithNoArchivedUsers_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockArchiveService
                .Setup(x => x.GetAllArchivedUsersAsync())
                .ReturnsAsync(new List<ArchiveUserDto>());

            // Act
            var result = await _controller.GetAllArchivedUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetArchivedUserById_WithValidId_ShouldReturnOkWithUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var archivedUser = new ArchiveUserDto
            {
                Id = userId,
                Username = "archived.user",
                Email = "user@test.com",
                FirstName = "John",
                LastName = "Doe",
                ArchivedAt = DateTime.UtcNow
            };

            _mockArchiveService
                .Setup(x => x.GetArchivedUserByIdAsync(userId))
                .ReturnsAsync(archivedUser);

            // Act
            var result = await _controller.GetArchivedUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetArchivedUserByIdAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task GetArchivedUserById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.GetArchivedUserByIdAsync(userId))
                .ReturnsAsync((ArchiveUserDto?)null);

            // Act
            var result = await _controller.GetArchivedUserById(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region Restore Tests

        [Fact]
        public async Task RestoreUser_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.RestoreUserAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RestoreUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.RestoreUserAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task RestoreUser_WithInvalidId_ShouldReturnBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.RestoreUserAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RestoreUser(userId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequestResult.StatusCode);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task PermanentDeleteUser_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.PermanentDeleteArchivedUserAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.PermanentDeleteUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.PermanentDeleteArchivedUserAsync(userId),
                Times.Once);
        }

        [Fact]
        public async Task PermanentDeleteUser_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.PermanentDeleteArchivedUserAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.PermanentDeleteUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion
    }
}
