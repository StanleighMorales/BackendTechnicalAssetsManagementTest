using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.DTOs.Item;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagement.src.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TechnicalAssetManagementApi.Dtos.Item;
using Xunit;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class ItemControllerTests
    {
        private readonly Mock<IItemService> _mockItemService;
        private readonly ItemController _controller;

        public ItemControllerTests()
        {
            _mockItemService = new Mock<IItemService>();
            _controller = new ItemController(_mockItemService.Object);
        }

        #region CreateItem Tests

        [Fact]
        public async Task CreateItem_WithValidData_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateItemsDto
            {
                ItemName = "Laptop",
                SerialNumber = "SN-001"
            };

            var createdItem = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                SerialNumber = "SN-001",
                Barcode = "ITEM-SN-001"
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(createDto))
                .ReturnsAsync(createdItem);

            // Act
            var result = await _controller.CreateItem(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item created successfully.", response.Message);
            Assert.Equal(createdItem.Id, response.Data.Id);
        }

        [Fact]
        public async Task CreateItem_WithDuplicateSerialNumber_ShouldReturnConflict()
        {
            // Arrange
            var createDto = new CreateItemsDto
            {
                ItemName = "Laptop",
                SerialNumber = "SN-001"
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(createDto))
                .ThrowsAsync(new ItemService.DuplicateSerialNumberException("Serial number already exists"));

            // Act
            var result = await _controller.CreateItem(createDto);

            // Assert
            var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(conflictResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Serial number already exists", response.Message);
        }

        [Fact]
        public async Task CreateItem_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateItemsDto
            {
                ItemName = "Laptop",
                SerialNumber = ""
            };

            _mockItemService
                .Setup(x => x.CreateItemAsync(createDto))
                .ThrowsAsync(new ArgumentException("Serial number is required"));

            // Act
            var result = await _controller.CreateItem(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Serial number is required", response.Message);
        }

        #endregion

        #region GetAllItems Tests

        [Fact]
        public async Task GetAllItems_ShouldReturnOkWithItems()
        {
            // Arrange
            var items = new List<ItemDto>
            {
                new ItemDto { Id = Guid.NewGuid(), ItemName = "Laptop", SerialNumber = "SN-001" },
                new ItemDto { Id = Guid.NewGuid(), ItemName = "Mouse", SerialNumber = "SN-002" }
            };

            _mockItemService
                .Setup(x => x.GetAllItemsAsync())
                .ReturnsAsync(items);

            // Act
            var result = await _controller.GetAllItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<ItemDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Items retrieved successfully.", response.Message);
            Assert.Equal(2, response.Data.Count());
        }

        [Fact]
        public async Task GetAllItems_WithEmptyList_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockItemService
                .Setup(x => x.GetAllItemsAsync())
                .ReturnsAsync(new List<ItemDto>());

            // Act
            var result = await _controller.GetAllItems();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<ItemDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Empty(response.Data);
        }

        #endregion

        #region GetItemById Tests

        [Fact]
        public async Task GetItemById_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new ItemDto
            {
                Id = itemId,
                ItemName = "Laptop",
                SerialNumber = "SN-001"
            };

            _mockItemService
                .Setup(x => x.GetItemByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetItemById(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item retrieved successfully.", response.Message);
            Assert.Equal(itemId, response.Data.Id);
        }

        [Fact]
        public async Task GetItemById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemService
                .Setup(x => x.GetItemByIdAsync(itemId))
                .ReturnsAsync((ItemDto)null);

            // Act
            var result = await _controller.GetItemById(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Item not found.", response.Message);
        }

        #endregion

        #region GetItemByBarcode Tests

        [Fact]
        public async Task GetItemByBarcode_WithValidBarcode_ShouldReturnOk()
        {
            // Arrange
            var barcode = "ITEM-SN-001";
            var item = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                Barcode = barcode
            };

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcode))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetItemByBarcode(barcode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item retrieved successfully.", response.Message);
            Assert.Equal(barcode, response.Data.Barcode);
        }

        [Fact]
        public async Task GetItemByBarcode_WithInvalidBarcode_ShouldReturnNotFound()
        {
            // Arrange
            var barcode = "INVALID-BARCODE";

            _mockItemService
                .Setup(x => x.GetItemByBarcodeAsync(barcode))
                .ReturnsAsync((ItemDto)null);

            // Act
            var result = await _controller.GetItemByBarcode(barcode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Item not found.", response.Message);
        }

        #endregion

        #region GetItemBySerialNumber Tests

        [Fact]
        public async Task GetItemBySerialNumber_WithValidSerialNumber_ShouldReturnOk()
        {
            // Arrange
            var serialNumber = "SN-001";
            var item = new ItemDto
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                SerialNumber = serialNumber
            };

            _mockItemService
                .Setup(x => x.GetItemBySerialNumberAsync(serialNumber))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetItemBySerialNumber(serialNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item retrieved successfully.", response.Message);
            Assert.Equal(serialNumber, response.Data.SerialNumber);
        }

        [Fact]
        public async Task GetItemBySerialNumber_WithInvalidSerialNumber_ShouldReturnNotFound()
        {
            // Arrange
            var serialNumber = "INVALID-SN";

            _mockItemService
                .Setup(x => x.GetItemBySerialNumberAsync(serialNumber))
                .ReturnsAsync((ItemDto)null);

            // Act
            var result = await _controller.GetItemBySerialNumber(serialNumber);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ItemDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Item not found.", response.Message);
        }

        #endregion

        #region UpdateItem Tests

        [Fact]
        public async Task UpdateItem_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateItemsDto
            {
                ItemName = "Updated Laptop",
                SerialNumber = "SN-001-UPDATED"
            };

            _mockItemService
                .Setup(x => x.UpdateItemAsync(itemId, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateItem(itemId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item updated successfully.", response.Message);
        }

        [Fact]
        public async Task UpdateItem_WithNonExistentItem_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateItemsDto();

            _mockItemService
                .Setup(x => x.UpdateItemAsync(itemId, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateItem(itemId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Update failed. Item not found.", response.Message);
        }

        [Fact]
        public async Task UpdateItem_WithInvalidFile_ShouldReturnBadRequest()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateItemsDto();

            _mockItemService
                .Setup(x => x.UpdateItemAsync(itemId, updateDto))
                .ThrowsAsync(new ArgumentException("Invalid file format"));

            // Act
            var result = await _controller.UpdateItem(itemId, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid file format", response.Message);
        }

        #endregion

        #region ArchiveItem Tests

        [Fact]
        public async Task ArchiveItem_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemService
                .Setup(x => x.DeleteItemAsync(itemId))
                .ReturnsAsync((true, string.Empty));

            // Act
            var result = await _controller.ArchiveItem(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item Archived successfully.", response.Message);
        }

        [Fact]
        public async Task ArchiveItem_WithNonExistentItem_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemService
                .Setup(x => x.DeleteItemAsync(itemId))
                .ReturnsAsync((false, "Item not found"));

            // Act
            var result = await _controller.ArchiveItem(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Item not found", response.Message);
        }

        [Fact]
        public async Task ArchiveItem_WithArchiveFailure_ShouldReturnBadRequest()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemService
                .Setup(x => x.DeleteItemAsync(itemId))
                .ReturnsAsync((false, "Archive operation failed"));

            // Act
            var result = await _controller.ArchiveItem(itemId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Archive operation failed", response.Message);
        }

        #endregion

        #region ImportItemsFromExcel Tests

        [Fact]
        public async Task ImportItemsFromExcel_WithValidFile_ShouldReturnOk()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("items.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var importResult = new ImportItemsResponseDto
            {
                SuccessCount = 10,
                FailureCount = 0,
                Errors = new List<string>()
            };

            _mockItemService
                .Setup(x => x.ImportItemsFromExcelAsync(mockFile.Object))
                .ReturnsAsync(importResult);

            // Act
            var result = await _controller.ImportItemsFromExcel(mockFile.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Contains("Success: 10", response.Message);
        }

        [Fact]
        public async Task ImportItemsFromExcel_WithNoFile_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.ImportItemsFromExcel(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("No file uploaded.", response.Message);
        }

        [Fact]
        public async Task ImportItemsFromExcel_WithInvalidFileExtension_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("items.txt");
            mockFile.Setup(f => f.Length).Returns(1024);

            // Act
            var result = await _controller.ImportItemsFromExcel(mockFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Only .xlsx files", response.Message);
        }

        [Fact]
        public async Task ImportItemsFromExcel_WithInvalidMimeType_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("items.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("text/plain");

            // Act
            var result = await _controller.ImportItemsFromExcel(mockFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid file type", response.Message);
        }

        [Fact]
        public async Task ImportItemsFromExcel_WithNoSuccessfulImports_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("items.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var importResult = new ImportItemsResponseDto
            {
                SuccessCount = 0,
                FailureCount = 5,
                Errors = new List<string> { "Error 1", "Error 2" }
            };

            _mockItemService
                .Setup(x => x.ImportItemsFromExcelAsync(mockFile.Object))
                .ReturnsAsync(importResult);

            // Act
            var result = await _controller.ImportItemsFromExcel(mockFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("No items were imported", response.Message);
        }

        [Fact]
        public async Task ImportItemsFromExcel_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("items.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            _mockItemService
                .Setup(x => x.ImportItemsFromExcelAsync(mockFile.Object))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.ImportItemsFromExcel(mockFile.Object);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ApiResponse<ImportItemsResponseDto>>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Database connection failed", response.Message);
        }

        #endregion
    }
}
