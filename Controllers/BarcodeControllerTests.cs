using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.DTOs.Item;
using BackendTechnicalAssetsManagement.src.IService;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class BarcodeControllerTests
    {
        private readonly Mock<IItemService> _mockItemService;
        private readonly BarcodeController _controller;

        public BarcodeControllerTests()
        {
            _mockItemService = new Mock<IItemService>();
            _controller = new BarcodeController(_mockItemService.Object);
        }

        #region GenerateBarcode Tests

        [Fact]
        public async Task GenerateBarcode_WithValidBarcode_ShouldReturnFileResult()
        {
            // Arrange
            var barcodeText = "ITEM-12345";
            var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header bytes
            var base64Image = Convert.ToBase64String(imageBytes);
            
            var itemDto = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Test Item",
                Barcode = barcodeText,
                BarcodeImage = $"data:image/png;base64,{base64Image}"
            };

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcodeText))
                .ReturnsAsync(itemDto);

            // Act
            var result = await _controller.GenerateBarcode(barcodeText);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.Equal(imageBytes, fileResult.FileContents);
            _mockItemService.Verify(x => x.GetItemByBarcodeAsync(barcodeText), Times.Once);
        }

        [Fact]
        public async Task GenerateBarcode_WithValidBarcodeWithoutPrefix_ShouldReturnFileResult()
        {
            // Arrange
            var barcodeText = "ITEM-67890";
            var imageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A };
            var base64Image = Convert.ToBase64String(imageBytes);
            
            var itemDto = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Another Item",
                Barcode = barcodeText,
                BarcodeImage = base64Image // No prefix
            };

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcodeText))
                .ReturnsAsync(itemDto);

            // Act
            var result = await _controller.GenerateBarcode(barcodeText);

            // Assert
            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("image/png", fileResult.ContentType);
            Assert.Equal(imageBytes, fileResult.FileContents);
        }

        [Fact]
        public async Task GenerateBarcode_WithNonExistentBarcode_ShouldReturnNotFound()
        {
            // Arrange
            var barcodeText = "ITEM-NONEXISTENT";
            
            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcodeText))
                .ReturnsAsync((ItemDto?)null);

            // Act
            var result = await _controller.GenerateBarcode(barcodeText);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains(barcodeText, notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task GenerateBarcode_WithEmptyBarcodeImage_ShouldReturnNotFound()
        {
            // Arrange
            var barcodeText = "ITEM-EMPTY";
            
            var itemDto = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Item Without Image",
                Barcode = barcodeText,
                BarcodeImage = string.Empty
            };

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcodeText))
                .ReturnsAsync(itemDto);

            // Act
            var result = await _controller.GenerateBarcode(barcodeText);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("missing", notFoundResult.Value?.ToString());
        }

        [Fact]
        public async Task GenerateBarcode_WithInvalidBase64_ShouldReturnBadRequest()
        {
            // Arrange
            var barcodeText = "ITEM-INVALID";
            
            var itemDto = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Item With Invalid Image",
                Barcode = barcodeText,
                BarcodeImage = "data:image/png;base64,INVALID_BASE64!!!"
            };

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcodeText))
                .ReturnsAsync(itemDto);

            // Act
            var result = await _controller.GenerateBarcode(barcodeText);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("corrupt", badRequestResult.Value?.ToString());
        }

        #endregion
    }
}
