using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.DTOs;
using BackendTechnicalAssetsManagement.src.DTOs.LentItems;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Utils;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class LentItemsControllerTests
    {
        private readonly Mock<ILentItemsService> _mockService;
        private readonly LentItemsController _controller;

        public LentItemsControllerTests()
        {
            _mockService = new Mock<ILentItemsService>();
            _controller = new LentItemsController(_mockService.Object);
        }

        #region Add Tests

        [Fact]
        public async Task Add_WithValidData_ShouldReturnCreatedResult()
        {
            // Arrange
            var createDto = new CreateLentItemDto
            {
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Pending"
            };

            var createdDto = new LentItemsDto
            {
                Id = Guid.NewGuid(),
                UserId = createDto.UserId,
                Status = "Pending"
            };

            _mockService.Setup(s => s.AddAsync(createDto))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.Add(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal(createdDto, response.Data);
            Assert.Equal("User - Item Listed Successfully.", response.Message);
        }

        #endregion

        #region AddForGuest Tests

        [Fact]
        public async Task AddForGuest_WithValidData_ShouldReturnCreatedResult()
        {
            // Arrange
            var createDto = new CreateLentItemsForGuestDto
            {
                ItemId = Guid.NewGuid(),
                BorrowerFirstName = "John",
                BorrowerLastName = "Doe",
                BorrowerRole = "Teacher",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10-11AM",
                Status = "Borrowed"
            };

            var createdDto = new LentItemsDto
            {
                Id = Guid.NewGuid(),
                Status = "Borrowed"
            };

            _mockService.Setup(s => s.AddForGuestAsync(createDto))
                .ReturnsAsync(createdDto);

            // Act
            var result = await _controller.AddForGuest(createDto);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(createdResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Guest - Item Listed Successfully.", response.Message);
        }

        [Fact]
        public async Task AddForGuest_StudentWithoutIdNumber_ShouldReturnBadRequest()
        {
            // Arrange
            var createDto = new CreateLentItemsForGuestDto
            {
                ItemId = Guid.NewGuid(),
                BorrowerFirstName = "Student",
                BorrowerLastName = "Name",
                BorrowerRole = "Student",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10-11AM",
                StudentIdNumber = null, // Missing required field
                Status = "Borrowed"
            };

            // Act
            var result = await _controller.AddForGuest(createDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Student ID number is required", response.Message);
        }

        #endregion

        #region GetAll Tests

        [Fact]
        public async Task GetAll_ShouldReturnOkWithItems()
        {
            // Arrange
            var items = new List<LentItemsDto>
            {
                new LentItemsDto { Id = Guid.NewGuid(), Status = "Borrowed" },
                new LentItemsDto { Id = Guid.NewGuid(), Status = "Returned" }
            };

            _mockService.Setup(s => s.GetAllAsync())
                .ReturnsAsync(items);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<LentItemsDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(2, response.Data!.Count());
        }

        #endregion

        #region GetById Tests

        [Fact]
        public async Task GetById_WithValidId_ShouldReturnOkWithItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new LentItemsDto { Id = itemId, Status = "Borrowed" };

            _mockService.Setup(s => s.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetById(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(itemId, response.Data!.Id);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(s => s.GetByIdAsync(itemId))
                .ReturnsAsync((LentItemsDto?)null);

            // Act
            var result = await _controller.GetById(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Item not found.", response.Message);
        }

        #endregion

        #region GetByBarcode Tests

        [Fact]
        public async Task GetByBarcode_WithValidBarcode_ShouldReturnOkWithItem()
        {
            // Arrange
            var barcode = "LENT-20251128-001";
            var item = new LentItemsDto { Id = Guid.NewGuid(), Barcode = barcode };

            _mockService.Setup(s => s.GetByBarcodeAsync(barcode))
                .ReturnsAsync(item);

            // Act
            var result = await _controller.GetByBarcode(barcode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(barcode, response.Data!.Barcode);
        }

        [Fact]
        public async Task GetByBarcode_WithInvalidFormat_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidBarcode = "INVALID-FORMAT";

            // Act
            var result = await _controller.GetByBarcode(invalidBarcode);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid barcode format", response.Message);
        }

        [Fact]
        public async Task GetByBarcode_WithWrongLength_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidBarcode = "LENT-2025-01";

            // Act
            var result = await _controller.GetByBarcode(invalidBarcode);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task GetByBarcode_NotFound_ShouldReturnNotFound()
        {
            // Arrange
            var barcode = "LENT-20251128-999";
            _mockService.Setup(s => s.GetByBarcodeAsync(barcode))
                .ReturnsAsync((LentItemsDto?)null);

            // Act
            var result = await _controller.GetByBarcode(barcode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<LentItemsDto>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task Update_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateLentItemDto { Status = "Returned" };

            _mockService.Setup(s => s.UpdateAsync(itemId, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Update(itemId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item updated successfully.", response.Message);
        }

        [Fact]
        public async Task Update_WithNonExistentItem_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateLentItemDto { Status = "Returned" };

            _mockService.Setup(s => s.UpdateAsync(itemId, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Update(itemId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region UpdateStatus Tests

        [Fact]
        public async Task UpdateStatus_WithValidBarcode_ShouldReturnOk()
        {
            // Arrange
            var barcode = "LENT-20251128-001";
            var scanDto = new ScanLentItemDto { LentItemsStatus = LentItemsStatus.Borrowed };

            _mockService.Setup(s => s.UpdateStatusByBarcodeAsync(barcode, scanDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStatus(barcode, scanDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task UpdateStatus_WithInvalidBarcodeFormat_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidBarcode = "INVALID";
            var scanDto = new ScanLentItemDto { LentItemsStatus = LentItemsStatus.Borrowed };

            // Act
            var result = await _controller.UpdateStatus(invalidBarcode, scanDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region GetByDateTime Tests

        [Fact]
        public async Task GetByDateTime_WithValidDate_ShouldReturnOkWithItems()
        {
            // Arrange
            var dateString = "2025-11-28";
            var items = new List<LentItemsDto>
            {
                new LentItemsDto { Id = Guid.NewGuid(), Status = "Borrowed" }
            };

            _mockService.Setup(s => s.GetByDateTimeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(items);

            // Act
            var result = await _controller.GetByDateTime(dateString);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<LentItemsDto>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Single(response.Data!);
        }

        [Fact]
        public async Task GetByDateTime_WithInvalidDateFormat_ShouldReturnBadRequest()
        {
            // Arrange
            var invalidDate = "not-a-date";

            // Act
            var result = await _controller.GetByDateTime(invalidDate);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<LentItemsDto>>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid date format", response.Message);
        }

        [Fact]
        public async Task GetByDateTime_WithNoResults_ShouldReturnNotFound()
        {
            // Arrange
            var dateString = "2025-12-25";
            _mockService.Setup(s => s.GetByDateTimeAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<LentItemsDto>());

            // Act
            var result = await _controller.GetByDateTime(dateString);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<LentItemsDto>>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region ReturnItemByItemBarcode Tests

        [Fact]
        public async Task ReturnItemByItemBarcode_WithValidBarcode_ShouldReturnOk()
        {
            // Arrange
            var itemBarcode = "ITEM-SN-001";
            _mockService.Setup(s => s.ReturnItemByItemBarcodeAsync(itemBarcode))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ReturnItemByItemBarcode(itemBarcode);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Item returned successfully.", response.Message);
        }

        [Fact]
        public async Task ReturnItemByItemBarcode_WithInvalidBarcode_ShouldReturnNotFound()
        {
            // Arrange
            var itemBarcode = "ITEM-NONEXISTENT";
            _mockService.Setup(s => s.ReturnItemByItemBarcodeAsync(itemBarcode))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ReturnItemByItemBarcode(itemBarcode);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region ArchiveLentItems Tests

        [Fact]
        public async Task ArchiveLentItems_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(s => s.ArchiveLentItems(itemId))
                .ReturnsAsync((true, string.Empty));

            // Act
            var result = await _controller.ArchiveLentItems(itemId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
        }

        [Fact]
        public async Task ArchiveLentItems_WithNonExistentId_ShouldReturnNotFound()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(s => s.ArchiveLentItems(itemId))
                .ReturnsAsync((false, "Item not found"));

            // Act
            var result = await _controller.ArchiveLentItems(itemId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        [Fact]
        public async Task ArchiveLentItems_WithNotReturnedItem_ShouldReturnBadRequest()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            _mockService.Setup(s => s.ArchiveLentItems(itemId))
                .ReturnsAsync((false, "Item must be returned before archiving"));

            // Act
            var result = await _controller.ArchiveLentItems(itemId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
        }

        #endregion
    }
}
