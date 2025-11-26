using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Items;
using BackendTechnicalAssetsManagement.src.DTOs.Item;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using TechnicalAssetManagementApi.Dtos.Item;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class ItemServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IWebHostEnvironment> _mockHostEnvironment;
        private readonly Mock<IArchiveItemsService> _mockArchiveItemsService;
        private readonly Mock<IBarcodeGeneratorService> _mockBarcodeGenerator;
        private readonly ItemService _itemService;

        public ItemServiceTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockHostEnvironment = new Mock<IWebHostEnvironment>();
            _mockArchiveItemsService = new Mock<IArchiveItemsService>();
            _mockBarcodeGenerator = new Mock<IBarcodeGeneratorService>();

            // Initialize the service with all mocks
            _itemService = new ItemService(
                _mockItemRepository.Object,
                _mockMapper.Object,
                _mockHostEnvironment.Object,
                _mockArchiveItemsService.Object,
                _mockBarcodeGenerator.Object
            );
        }

        #region CreateItemAsync Tests

        [Fact]
        public async Task CreateItemAsync_WithValidData_ShouldCreateItem()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell",
                Description = "Test Description"
            };

            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell",
                Description = "Test Description",
                Barcode = "ITEM-SN-12345",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var itemDto = new ItemDto
            {
                Id = newItem.Id,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop"
            };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-12345"))
                .ReturnsAsync((Item?)null);

            _mockMapper
                .Setup(x => x.Map<Item>(createItemDto))
                .Returns(newItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ReturnsAsync(newItem);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(itemDto);

            // Act
            var result = await _itemService.CreateItemAsync(createItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SN-12345", result.SerialNumber);
            Assert.Equal("Test Item", result.ItemName);
            _mockItemRepository.Verify(x => x.AddAsync(It.IsAny<Item>()), Times.Once);
            _mockItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateItemAsync_WithSerialNumberAlreadyHavingPrefix_ShouldNotAddPrefixAgain()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto
            {
                Id = newItem.Id,
                SerialNumber = "SN-12345"
            };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-12345"))
                .ReturnsAsync((Item?)null);

            _mockMapper
                .Setup(x => x.Map<Item>(createItemDto))
                .Returns(newItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ReturnsAsync(newItem);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(itemDto);

            // Act
            var result = await _itemService.CreateItemAsync(createItemDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SN-12345", result.SerialNumber);
            Assert.DoesNotContain("SN-SN-", result.SerialNumber);
        }

        [Fact]
        public async Task CreateItemAsync_WithEmptySerialNumber_ShouldThrowArgumentException()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "",
                ItemName = "Test Item"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _itemService.CreateItemAsync(createItemDto));
            Assert.Equal("SerialNumber cannot be empty.", exception.Message);
        }

        [Fact]
        public async Task CreateItemAsync_WithDuplicateSerialNumber_ShouldThrowDuplicateSerialNumberException()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "12345",
                ItemName = "Test Item"
            };

            var existingItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Existing Item"
            };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-12345"))
                .ReturnsAsync(existingItem);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ItemService.DuplicateSerialNumberException>(
                () => _itemService.CreateItemAsync(createItemDto));
            Assert.Contains("already exists", exception.Message);
        }

        [Fact]
        public async Task CreateItemAsync_ShouldGenerateBarcodeText()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto
            {
                Id = newItem.Id,
                SerialNumber = "SN-12345",
                Barcode = "ITEM-SN-12345"
            };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-12345"))
                .ReturnsAsync((Item?)null);

            _mockBarcodeGenerator
                .Setup(x => x.GenerateItemBarcode("SN-12345"))
                .Returns("ITEM-SN-12345");

            _mockBarcodeGenerator
                .Setup(x => x.GenerateBarcodeImage("ITEM-SN-12345"))
                .Returns(new byte[] { 1, 2, 3 });

            _mockMapper
                .Setup(x => x.Map<Item>(createItemDto))
                .Returns(newItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ReturnsAsync(newItem);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(itemDto);

            // Act
            var result = await _itemService.CreateItemAsync(createItemDto);

            // Assert
            _mockBarcodeGenerator.Verify(x => x.GenerateItemBarcode("SN-12345"), Times.Once);
            _mockBarcodeGenerator.Verify(x => x.GenerateBarcodeImage("ITEM-SN-12345"), Times.Once);
        }

        [Fact]
        public async Task CreateItemAsync_ShouldSetStatusToAvailable()
        {
            // Arrange
            var createItemDto = new CreateItemsDto
            {
                SerialNumber = "12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            Item? capturedItem = null;
            var newItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto { Id = newItem.Id };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-12345"))
                .ReturnsAsync((Item?)null);

            _mockBarcodeGenerator
                .Setup(x => x.GenerateItemBarcode(It.IsAny<string>()))
                .Returns("ITEM-SN-12345");

            _mockBarcodeGenerator
                .Setup(x => x.GenerateBarcodeImage(It.IsAny<string>()))
                .Returns(new byte[] { 1, 2, 3 });

            _mockMapper
                .Setup(x => x.Map<Item>(createItemDto))
                .Returns(newItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .Callback<Item>(item => capturedItem = item)
                .ReturnsAsync(newItem);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(itemDto);

            // Act
            await _itemService.CreateItemAsync(createItemDto);

            // Assert
            Assert.NotNull(capturedItem);
            Assert.Equal(ItemStatus.Available, capturedItem.Status);
        }

        #endregion

        #region GetAllItemsAsync Tests

        [Fact]
        public async Task GetAllItemsAsync_ShouldReturnAllItems()
        {
            // Arrange
            var items = new List<Item>
            {
                new Item { Id = Guid.NewGuid(), SerialNumber = "SN-001", ItemName = "Item 1" },
                new Item { Id = Guid.NewGuid(), SerialNumber = "SN-002", ItemName = "Item 2" }
            };

            var itemDtos = new List<ItemDto>
            {
                new ItemDto { Id = items[0].Id, SerialNumber = "SN-001", ItemName = "Item 1" },
                new ItemDto { Id = items[1].Id, SerialNumber = "SN-002", ItemName = "Item 2" }
            };

            _mockItemRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(items);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ItemDto>>(items))
                .Returns(itemDtos);

            // Act
            var result = await _itemService.GetAllItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllItemsAsync_WithEmptyList_ShouldReturnEmptyCollection()
        {
            // Arrange
            var items = new List<Item>();
            var itemDtos = new List<ItemDto>();

            _mockItemRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(items);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ItemDto>>(items))
                .Returns(itemDtos);

            // Act
            var result = await _itemService.GetAllItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetItemByIdAsync Tests

        [Fact]
        public async Task GetItemByIdAsync_WithValidId_ShouldReturnItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(item))
                .Returns(itemDto);

            // Act
            var result = await _itemService.GetItemByIdAsync(itemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.Id);
            Assert.Equal("SN-12345", result.SerialNumber);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }

        [Fact]
        public async Task GetItemByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns((ItemDto?)null);

            // Act
            var result = await _itemService.GetItemByIdAsync(itemId);

            // Assert
            Assert.Null(result);
            _mockItemRepository.Verify(x => x.GetByIdAsync(itemId), Times.Once);
        }

        #endregion

        #region GetItemByBarcodeAsync Tests

        [Fact]
        public async Task GetItemByBarcodeAsync_WithValidBarcode_ShouldReturnItem()
        {
            // Arrange
            var barcode = "ITEM-SN-12345";
            var item = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                Barcode = barcode,
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto
            {
                Id = item.Id,
                SerialNumber = "SN-12345",
                Barcode = barcode,
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(item);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(item))
                .Returns(itemDto);

            // Act
            var result = await _itemService.GetItemByBarcodeAsync(barcode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(barcode, result.Barcode);
            _mockItemRepository.Verify(x => x.GetByBarcodeAsync(barcode), Times.Once);
        }

        [Fact]
        public async Task GetItemByBarcodeAsync_WithInvalidBarcode_ShouldReturnNull()
        {
            // Arrange
            var barcode = "INVALID-BARCODE";

            _mockItemRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync((Item?)null);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns((ItemDto?)null);

            // Act
            var result = await _itemService.GetItemByBarcodeAsync(barcode);

            // Assert
            Assert.Null(result);
            _mockItemRepository.Verify(x => x.GetByBarcodeAsync(barcode), Times.Once);
        }

        #endregion

        #region GetItemBySerialNumberAsync Tests

        [Fact]
        public async Task GetItemBySerialNumberAsync_WithValidSerialNumber_ShouldReturnItem()
        {
            // Arrange
            var serialNumber = "SN-12345";
            var item = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = serialNumber,
                ItemName = "Test Item"
            };

            var itemDto = new ItemDto
            {
                Id = item.Id,
                SerialNumber = serialNumber,
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync(serialNumber))
                .ReturnsAsync(item);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(item))
                .Returns(itemDto);

            // Act
            var result = await _itemService.GetItemBySerialNumberAsync(serialNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serialNumber, result.SerialNumber);
            _mockItemRepository.Verify(x => x.GetBySerialNumberAsync(serialNumber), Times.Once);
        }

        [Fact]
        public async Task GetItemBySerialNumberAsync_WithInvalidSerialNumber_ShouldReturnNull()
        {
            // Arrange
            var serialNumber = "SN-INVALID";

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync(serialNumber))
                .ReturnsAsync((Item?)null);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns((ItemDto?)null);

            // Act
            var result = await _itemService.GetItemBySerialNumberAsync(serialNumber);

            // Assert
            Assert.Null(result);
            _mockItemRepository.Verify(x => x.GetBySerialNumberAsync(serialNumber), Times.Once);
        }

        #endregion

        #region UpdateItemAsync Tests

        [Fact]
        public async Task UpdateItemAsync_WithValidData_ShouldUpdateItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var existingItem = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Old Name",
                Barcode = "ITEM-SN-12345"
            };

            var updateDto = new UpdateItemsDto
            {
                ItemName = "New Name",
                ItemType = "Updated Type"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingItem))
                .Callback<UpdateItemsDto, Item>((src, dest) =>
                {
                    dest.ItemName = src.ItemName!;
                    dest.ItemType = src.ItemType!;
                });

            _mockItemRepository
                .Setup(x => x.UpdateAsync(existingItem))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, updateDto);

            // Assert
            Assert.True(result);
            _mockItemRepository.Verify(x => x.UpdateAsync(existingItem), Times.Once);
            _mockItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_WithNonExistentItem_ShouldReturnFalse()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var updateDto = new UpdateItemsDto
            {
                ItemName = "New Name"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, updateDto);

            // Assert
            Assert.False(result);
            _mockItemRepository.Verify(x => x.UpdateAsync(It.IsAny<Item>()), Times.Never);
        }

        [Fact]
        public async Task UpdateItemAsync_WithNewSerialNumber_ShouldRegenerateBarcode()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var existingItem = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                Barcode = "ITEM-SN-12345",
                ItemName = "Test Item"
            };

            var updateDto = new UpdateItemsDto
            {
                SerialNumber = "67890"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-67890"))
                .ReturnsAsync((Item?)null);

            _mockBarcodeGenerator
                .Setup(x => x.GenerateItemBarcode("SN-67890"))
                .Returns("ITEM-SN-67890");

            _mockMapper
                .Setup(x => x.Map(updateDto, existingItem))
                .Callback<UpdateItemsDto, Item>((src, dest) => { });

            _mockItemRepository
                .Setup(x => x.UpdateAsync(existingItem))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("SN-67890", existingItem.SerialNumber);
            Assert.NotNull(existingItem.Barcode);
            _mockItemRepository.Verify(x => x.UpdateAsync(existingItem), Times.Once);
        }

        [Fact]
        public async Task UpdateItemAsync_WithDuplicateSerialNumber_ShouldThrowException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var existingItem = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            var anotherItem = new Item
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-67890",
                ItemName = "Another Item"
            };

            var updateDto = new UpdateItemsDto
            {
                SerialNumber = "67890"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockItemRepository
                .Setup(x => x.GetBySerialNumberAsync("SN-67890"))
                .ReturnsAsync(anotherItem);

            // Act & Assert
            await Assert.ThrowsAsync<ItemService.DuplicateSerialNumberException>(
                () => _itemService.UpdateItemAsync(itemId, updateDto));
        }

        [Fact]
        public async Task UpdateItemAsync_WithoutSerialNumberChange_ShouldNotRegenerateBarcode()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var existingItem = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                Barcode = "ITEM-SN-12345",
                ItemName = "Test Item"
            };

            var updateDto = new UpdateItemsDto
            {
                ItemName = "Updated Name"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingItem))
                .Callback<UpdateItemsDto, Item>((src, dest) =>
                {
                    dest.ItemName = src.ItemName!;
                });

            _mockItemRepository
                .Setup(x => x.UpdateAsync(existingItem))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, updateDto);

            // Assert
            Assert.True(result);
            Assert.Equal("ITEM-SN-12345", existingItem.Barcode);
            _mockBarcodeGenerator.Verify(x => x.GenerateItemBarcode(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateItemAsync_ShouldUpdateTimestamp()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var existingItem = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                UpdatedAt = DateTime.Now.AddDays(-1)
            };

            var updateDto = new UpdateItemsDto
            {
                ItemName = "Updated Name"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(existingItem);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingItem))
                .Callback<UpdateItemsDto, Item>((src, dest) =>
                {
                    dest.ItemName = src.ItemName!;
                });

            _mockItemRepository
                .Setup(x => x.UpdateAsync(existingItem))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            var beforeUpdate = DateTime.Now;

            // Act
            var result = await _itemService.UpdateItemAsync(itemId, updateDto);

            // Assert
            Assert.True(result);
            Assert.True(existingItem.UpdatedAt >= beforeUpdate);
        }

        #endregion

        #region DeleteItemAsync Tests

        [Fact]
        public async Task DeleteItemAsync_WithValidId_ShouldArchiveAndDeleteItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var itemToDelete = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                Status = ItemStatus.Available
            };

            var archiveDto = new CreateArchiveItemsDto
            {
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(itemToDelete);

            _mockMapper
                .Setup(x => x.Map<CreateArchiveItemsDto>(itemToDelete))
                .Returns(archiveDto);

            _mockArchiveItemsService
                .Setup(x => x.CreateItemArchiveAsync(archiveDto))
                .ReturnsAsync(new ArchiveItemsDto { Id = itemId, SerialNumber = "SN-12345" });

            _mockItemRepository
                .Setup(x => x.DeleteAsync(itemId))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _itemService.DeleteItemAsync(itemId);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.ErrorMessage);
            Assert.Equal(ItemStatus.Archived, itemToDelete.Status);
            _mockArchiveItemsService.Verify(x => x.CreateItemArchiveAsync(It.IsAny<CreateArchiveItemsDto>()), Times.Once);
            _mockItemRepository.Verify(x => x.DeleteAsync(itemId), Times.Once);
            _mockItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteItemAsync_WithNonExistentItem_ShouldReturnFailure()
        {
            // Arrange
            var itemId = Guid.NewGuid();

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _itemService.DeleteItemAsync(itemId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Item not found.", result.ErrorMessage);
            _mockItemRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteItemAsync_WhenArchiveFails_ShouldReturnFailureWithErrorMessage()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var itemToDelete = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(itemToDelete);

            _mockMapper
                .Setup(x => x.Map<CreateArchiveItemsDto>(itemToDelete))
                .Returns(new CreateArchiveItemsDto());

            _mockArchiveItemsService
                .Setup(x => x.CreateItemArchiveAsync(It.IsAny<CreateArchiveItemsDto>()))
                .ThrowsAsync(new Exception("Archive service error"));

            // Act
            var result = await _itemService.DeleteItemAsync(itemId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Archive operation failed", result.ErrorMessage);
            _mockItemRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task DeleteItemAsync_WhenSaveChangesFails_ShouldReturnFailure()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var itemToDelete = new Item
            {
                Id = itemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                Status = ItemStatus.Available
            };

            var archiveDto = new CreateArchiveItemsDto
            {
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(itemToDelete);

            _mockMapper
                .Setup(x => x.Map<CreateArchiveItemsDto>(itemToDelete))
                .Returns(archiveDto);

            _mockArchiveItemsService
                .Setup(x => x.CreateItemArchiveAsync(archiveDto))
                .ReturnsAsync(new ArchiveItemsDto { Id = itemId });

            _mockItemRepository
                .Setup(x => x.DeleteAsync(itemId))
                .Returns(Task.CompletedTask);

            _mockItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _itemService.DeleteItemAsync(itemId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to save changes", result.ErrorMessage);
        }

        #endregion

        #region ImportItemsFromExcelAsync Tests

        // Note: Since ItemService uses ExcelDataReader directly (not through IExcelReaderService),
        // these tests require actual Excel file format. The service should be refactored to use
        // IExcelReaderService for better testability, similar to how UserService does it.
        // 
        // Current implementation in ItemService.ImportItemsFromExcelAsync():
        // - Directly uses ExcelReaderFactory.CreateReader() 
        // - Should be refactored to use IExcelReaderService.ReadItemsFromExcelAsync()
        //
        // Once refactored, we can add comprehensive tests like:
        // - Valid Excel import with multiple items
        // - Missing required columns (SerialNumber)
        // - Duplicate serial numbers handling
        // - Invalid image paths (should continue without image)
        // - Error handling and reporting
        // - Barcode generation for each item
        // - Status set to Available for all items

        #endregion


    }
}
