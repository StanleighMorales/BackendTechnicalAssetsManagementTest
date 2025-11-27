using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.LentItems;
using BackendTechnicalAssetsManagement.src.IService;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class ArchiveLentItemsControllerTests
    {
        private readonly Mock<IArchiveLentItemsService> _mockArchiveService;
        private readonly ArchiveLentItemsController _controller;

        public ArchiveLentItemsControllerTests()
        {
            _mockArchiveService = new Mock<IArchiveLentItemsService>();
            var mockLogger = new Mock<ILogger<ArchiveLentItemsController>>();
            _controller = new ArchiveLentItemsController(_mockArchiveService.Object, mockLogger.Object);
        }

        #region GetAll Tests

        [Fact]
        public async Task GetAllLentItemsArchives_WithArchivedLentItems_ShouldReturnOkWithList()
        {
            // Arrange
            var archivedLentItems = new List<ArchiveLentItemsDto>
            {
                new ArchiveLentItemsDto
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Status = "Returned",
                    Barcode = "LENT-20251127-001",
                    CreatedAt = DateTime.UtcNow
                },
                new ArchiveLentItemsDto
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    Status = "Returned",
                    Barcode = "LENT-20251127-002",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _mockArchiveService
                .Setup(x => x.GetAllLentItemsArchivesAsync())
                .ReturnsAsync(archivedLentItems);

            // Act
            var result = await _controller.GetAllLentItemsArchives();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetAllLentItemsArchivesAsync(),
                Times.Once);
        }

        [Fact]
        public async Task GetAllLentItemsArchives_WithNoArchivedLentItems_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockArchiveService
                .Setup(x => x.GetAllLentItemsArchivesAsync())
                .ReturnsAsync(new List<ArchiveLentItemsDto>());

            // Act
            var result = await _controller.GetAllLentItemsArchives();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetLentItemsArchiveById_WithValidId_ShouldReturnOkWithLentItem()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var archivedLentItem = new ArchiveLentItemsDto
            {
                Id = lentItemId,
                UserId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-20251127-001",
                CreatedAt = DateTime.UtcNow
            };

            _mockArchiveService
                .Setup(x => x.GetLentItemsArchiveByIdAsync(lentItemId))
                .ReturnsAsync(archivedLentItem);

            // Act
            var result = await _controller.GetLentItemsArchiveById(lentItemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.GetLentItemsArchiveByIdAsync(lentItemId),
                Times.Once);
        }

        [Fact]
        public async Task GetLentItemsArchiveById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.GetLentItemsArchiveByIdAsync(lentItemId))
                .ReturnsAsync((ArchiveLentItemsDto?)null);

            // Act
            var result = await _controller.GetLentItemsArchiveById(lentItemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region Restore Tests

        [Fact]
        public async Task RestoreArchivedLentItems_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var restoredLentItem = new ArchiveLentItemsDto
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-20251127-001"
            };

            _mockArchiveService
                .Setup(x => x.RestoreLentItemsAsync(lentItemId))
                .ReturnsAsync(restoredLentItem);

            // Act
            var result = await _controller.RestoreArchivedLentItems(lentItemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.RestoreLentItemsAsync(lentItemId),
                Times.Once);
        }

        [Fact]
        public async Task RestoreArchivedLentItems_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.RestoreLentItemsAsync(lentItemId))
                .ReturnsAsync((ArchiveLentItemsDto?)null);

            // Act
            var result = await _controller.RestoreArchivedLentItems(lentItemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteLentItemsArchive_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.DeleteLentItemsArchiveAsync(lentItemId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteLentItemsArchive(lentItemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
            
            _mockArchiveService.Verify(
                x => x.DeleteLentItemsArchiveAsync(lentItemId),
                Times.Once);
        }

        [Fact]
        public async Task DeleteLentItemsArchive_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            _mockArchiveService
                .Setup(x => x.DeleteLentItemsArchiveAsync(lentItemId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteLentItemsArchive(lentItemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        #endregion
    }
}
