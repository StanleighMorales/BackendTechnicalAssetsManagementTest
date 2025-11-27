using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Items;
using BackendTechnicalAssetsManagement.src.DTOs.Item;
using BackendTechnicalAssetsManagement.src.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class ArchiveItemsControllerTests
    {
        private readonly Mock<IArchiveItemsService> _mockArchiveService;
        private readonly ArchiveItemsController _controller;

        public ArchiveItemsControllerTests()
        {
            _mockArchiveService = new Mock<IArchiveItemsService>();
            var mockLogger = new Mock<ILogger<ArchiveItemsController>>();
            _controller = new ArchiveItemsController(_mockArchiveService.Object, mockLogger.Object);
        }

        #region GetAll Tests

        [Fact]
        public async Task GetAllItemArchives_WithArchivedItems_ShouldReturnOkWithList()
        {
            // Arrange
            var archivedItems = new List<ArchiveItemsDto>
            {
                new ArchiveItemsDto
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Archived Laptop",
                    SerialNumber = "SN-001",
                    ArchivedAt = DateTime.UtcNow
                },
                new ArchiveItemsDto
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Archived Mouse",
                    SerialNumber = "SN-002",
                    ArchivedAt = DateTime.UtcNow
                }
            };

            _mockArchiveService
                .Setup(x => x.GetAllItemArchivesAsync())
                .ReturnsAsync(archivedItems);

            // Act
            var result = await _controller.GetAllItemArchives();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetAllItemArchivesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetAllItemArchives_WithNoArchivedItems_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockArchiveService
                .Setup(x => x.GetAllItemArchivesAsync())
                .ReturnsAsync(new List<ArchiveItemsDto>());

            // Act
            var result = await _controller.GetAllItemArchives();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetArchivedItemById_WithValidId_ShouldReturnOkWithItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var archivedItem = new ArchiveItemsDto
            {
                Id = itemId,
                ItemName = "Archived Laptop",
                SerialNumber = "SN-001",
                ArchivedAt = DateTime.UtcNow
            };

            _mockArchiveService
                .Setup(x => x.GetItemArchiveByIdAsync(itemId))
                .ReturnsAsync(archivedItem);

            // Act
            var result = await _controller.GetArchivedItemById(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetItemArchiveByIdAsync(itemId),
                Times.Once);
        }

        [Fact]
        public async Task GetArchivedItemById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.GetItemArchiveByIdAsync(itemId))
                .ReturnsAsync((ArchiveItemsDto?)null);

            // Act
            var result = await _controller.GetArchivedItemById(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region Restore Tests

        [Fact]
        public async Task RestoreArchivedItem_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var restoredItem = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Restored Laptop",
                SerialNumber = "SN-001",
                Status = ItemStatus.Available
            };

            _mockArchiveService
                .Setup(x => x.RestoreItemAsync(itemId))
                .ReturnsAsync(restoredItem);

            // Act
            var result = await _controller.RestoreArchivedItem(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.RestoreItemAsync(itemId),
                Times.Once);
        }

        [Fact]
        public async Task RestoreArchivedItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.RestoreItemAsync(itemId))
                .ReturnsAsync((ItemDto?)null);

            // Act
            var result = await _controller.RestoreArchivedItem(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteArchivedItem_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.DeleteItemArchiveAsync(itemId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteArchivedItem(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.DeleteItemArchiveAsync(itemId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteArchivedItem_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.DeleteItemArchiveAsync(itemId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteArchivedItem(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion
    }
}
