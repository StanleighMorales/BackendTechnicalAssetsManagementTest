using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.LentItems;
using BackendTechnicalAssetsManagement.src.DTOs.LentItems;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class LentItemsServiceTests
    {
        private readonly Mock<ILentItemsRepository> _mockLentItemsRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IArchiveLentItemsService> _mockArchiveLentItemsService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly LentItemsService _lentItemsService;

        public LentItemsServiceTests()
        {
            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockArchiveLentItemsService = new Mock<IArchiveLentItemsService>();
            _mockMapper = new Mock<IMapper>();

            // Setup GetDbContext to return null (not needed for most tests)
            _mockLentItemsRepository
                .Setup(x => x.GetDbContext())
                .Returns((BackendTechnicalAssetsManagement.src.Data.AppDbContext)null!);

            // Initialize the service with all mocks
            _lentItemsService = new LentItemsService(
                _mockLentItemsRepository.Object,
                _mockMapper.Object,
                _mockUserRepository.Object,
                _mockItemRepository.Object,
                _mockArchiveLentItemsService.Object
            );
        }

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithDefectiveItem_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Broken Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Defective
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddAsync(createDto));
            Assert.Contains("Defective", exception.Message);
            Assert.Contains("cannot be lent", exception.Message);
        }

        [Fact]
        public async Task AddAsync_WithUnavailableItem_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Unavailable Laptop",
                Status = ItemStatus.Unavailable,
                Condition = ItemCondition.Good
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddAsync(createDto));
            Assert.Contains("already unavailable", exception.Message);
        }

        [Fact]
        public async Task AddAsync_WithAlreadyLentItem_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };
            var existingLentItem = LentItemsMockData.GetMockLentItem(Guid.NewGuid(), "Borrowed");
            existingLentItem.ItemId = itemId;

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems> { existingLentItem });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddAsync(createDto));
            Assert.Contains("already has an active lent record", exception.Message);
        }

        [Fact]
        public async Task AddAsync_WithNonExistentItem_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync((Item?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _lentItemsService.AddAsync(createDto));
        }

        [Fact]
        public async Task AddAsync_WithNonExistentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId, userId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems>());

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(new LentItems());

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _lentItemsService.AddAsync(createDto));
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllLentItems()
        {
            // Arrange
            var lentItems = LentItemsMockData.GetMockLentItemsList();
            var lentItemDtos = LentItemsMockData.GetMockLentItemsDtoList();

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(lentItems);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<LentItemsDto>>(lentItems))
                .Returns(lentItemDtos);

            // Act
            var result = await _lentItemsService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            _mockLentItemsRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnLentItem()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId);
            var lentItemDto = LentItemsMockData.GetMockLentItemsDto(lentItemId);

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto?>(lentItem))
                .Returns(lentItemDto);

            // Act
            var result = await _lentItemsService.GetByIdAsync(lentItemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lentItemId, result.Id);
            _mockLentItemsRepository.Verify(x => x.GetByIdAsync(lentItemId), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync((LentItems?)null);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto?>(It.IsAny<LentItems>()))
                .Returns((LentItemsDto?)null);

            // Act
            var result = await _lentItemsService.GetByIdAsync(lentItemId);

            // Assert
            Assert.Null(result);
            _mockLentItemsRepository.Verify(x => x.GetByIdAsync(lentItemId), Times.Once);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidData_ShouldUpdateLentItem()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var existingLentItem = LentItemsMockData.GetMockLentItem(lentItemId, "Borrowed");
            var updateDto = LentItemsMockData.GetValidUpdateLentItemDto();
            var item = new Item
            {
                Id = existingLentItem.ItemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Unavailable,
                Condition = ItemCondition.Good
            };

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(existingLentItem);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(existingLentItem.ItemId))
                .ReturnsAsync(item);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingLentItem))
                .Callback<UpdateLentItemDto, LentItems>((src, dest) =>
                {
                    dest.Room = src.Room!;
                    dest.Status = src.Status;
                });

            _mockLentItemsRepository
                .Setup(x => x.UpdateAsync(existingLentItem))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.UpdateAsync(lentItemId, updateDto);

            // Assert
            Assert.True(result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(existingLentItem), Times.Once);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WithNonExistentLentItem_ShouldReturnFalse()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var updateDto = LentItemsMockData.GetValidUpdateLentItemDto();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync((LentItems?)null);

            // Act
            var result = await _lentItemsService.UpdateAsync(lentItemId, updateDto);

            // Assert
            Assert.False(result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(It.IsAny<LentItems>()), Times.Never);
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_ToReturned_ShouldUpdateStatusAndSetItemAvailable()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId, "Borrowed");
            var item = new Item
            {
                Id = lentItem.ItemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Unavailable,
                Condition = ItemCondition.Good
            };
            var scanDto = LentItemsMockData.GetValidScanLentItemDto(LentItemsStatus.Returned);

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(lentItem.ItemId))
                .ReturnsAsync(item);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(item))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.UpdateAsync(lentItem))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.UpdateStatusAsync(lentItemId, scanDto);

            // Assert
            Assert.True(result);
            Assert.Equal("Returned", lentItem.Status);
            Assert.NotNull(lentItem.ReturnedAt);
            Assert.Equal(ItemStatus.Available, item.Status);
            _mockItemRepository.Verify(x => x.UpdateAsync(item), Times.Once);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(lentItem), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentLentItem_ShouldReturnFalse()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var scanDto = LentItemsMockData.GetValidScanLentItemDto();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync((LentItems?)null);

            // Act
            var result = await _lentItemsService.UpdateStatusAsync(lentItemId, scanDto);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateHistoryVisibility Tests

        [Fact]
        public async Task UpdateHistoryVisibility_WithValidUserAndItem_ShouldUpdateVisibility()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId);
            lentItem.UserId = userId;

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.UpdateAsync(lentItem))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.UpdateHistoryVisibility(lentItemId, userId, true);

            // Assert
            Assert.True(result);
            Assert.True(lentItem.IsHiddenFromUser);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(lentItem), Times.Once);
        }

        [Fact]
        public async Task UpdateHistoryVisibility_WithUnauthorizedUser_ShouldReturnFalse()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var differentUserId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId);
            lentItem.UserId = differentUserId;

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            // Act
            var result = await _lentItemsService.UpdateHistoryVisibility(lentItemId, userId, true);

            // Assert
            Assert.False(result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(It.IsAny<LentItems>()), Times.Never);
        }

        #endregion

        #region ReturnItemByItemBarcodeAsync Tests

        [Fact]
        public async Task ReturnItemByItemBarcodeAsync_WithValidBarcode_ShouldReturnItem()
        {
            // Arrange
            var itemBarcode = "ITEM-001";
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                Barcode = itemBarcode,
                ItemName = "Test Laptop",
                Status = ItemStatus.Unavailable
            };
            var lentItem = LentItemsMockData.GetMockLentItem(Guid.NewGuid(), "Borrowed");
            lentItem.ItemId = itemId;

            _mockItemRepository
                .Setup(x => x.GetByBarcodeAsync(itemBarcode))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems> { lentItem });

            _mockItemRepository
                .Setup(x => x.UpdateAsync(item))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.UpdateAsync(lentItem))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.ReturnItemByItemBarcodeAsync(itemBarcode);

            // Assert
            Assert.True(result);
            Assert.Equal("Returned", lentItem.Status);
            Assert.NotNull(lentItem.ReturnedAt);
            Assert.Equal(ItemStatus.Available, item.Status);
        }

        [Fact]
        public async Task ReturnItemByItemBarcodeAsync_WithNonExistentBarcode_ShouldReturnFalse()
        {
            // Arrange
            var itemBarcode = "INVALID-BARCODE";

            _mockItemRepository
                .Setup(x => x.GetByBarcodeAsync(itemBarcode))
                .ReturnsAsync((Item?)null);

            // Act
            var result = await _lentItemsService.ReturnItemByItemBarcodeAsync(itemBarcode);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region ArchiveLentItems Tests

        [Fact]
        public async Task ArchiveLentItems_WithValidId_ShouldArchiveAndDelete()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockReturnedLentItem(lentItemId);
            var archiveDto = new CreateArchiveLentItemsDto();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<CreateArchiveLentItemsDto>(lentItem))
                .Returns(archiveDto);

            _mockArchiveLentItemsService
                .Setup(x => x.CreateLentItemsArchiveAsync(archiveDto))
                .ReturnsAsync(new ArchiveLentItemsDto());

            _mockLentItemsRepository
                .Setup(x => x.PermaDeleteAsync(lentItemId))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.ArchiveLentItems(lentItemId);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.ErrorMessage);
            _mockArchiveLentItemsService.Verify(x => x.CreateLentItemsArchiveAsync(archiveDto), Times.Once);
            _mockLentItemsRepository.Verify(x => x.PermaDeleteAsync(lentItemId), Times.Once);
        }

        [Fact]
        public async Task ArchiveLentItems_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync((LentItems?)null);

            // Act
            var result = await _lentItemsService.ArchiveLentItems(lentItemId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Lent item not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task ArchiveLentItems_WithNotReturnedItem_ShouldSetItemAvailable()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId, "Borrowed");
            var item = new Item
            {
                Id = lentItem.ItemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Unavailable
            };
            var archiveDto = new CreateArchiveLentItemsDto();

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItemId))
                .ReturnsAsync(lentItem);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(lentItem.ItemId))
                .ReturnsAsync(item);

            _mockMapper
                .Setup(x => x.Map<CreateArchiveLentItemsDto>(lentItem))
                .Returns(archiveDto);

            _mockArchiveLentItemsService
                .Setup(x => x.CreateLentItemsArchiveAsync(archiveDto))
                .ReturnsAsync(new ArchiveLentItemsDto());

            _mockItemRepository
                .Setup(x => x.UpdateAsync(item))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.PermaDeleteAsync(lentItemId))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.ArchiveLentItems(lentItemId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(ItemStatus.Available, item.Status);
            _mockItemRepository.Verify(x => x.UpdateAsync(item), Times.Once);
        }

        #endregion

        #region SoftDeleteAsync and PermaDeleteAsync Tests

        [Fact]
        public async Task SoftDeleteAsync_WithValidId_ShouldSoftDelete()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();

            _mockLentItemsRepository
                .Setup(x => x.SoftDeleteAsync(lentItemId))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.SoftDeleteAsync(lentItemId);

            // Assert
            Assert.True(result);
            _mockLentItemsRepository.Verify(x => x.SoftDeleteAsync(lentItemId), Times.Once);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PermaDeleteAsync_WithValidId_ShouldPermanentlyDelete()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();

            _mockLentItemsRepository
                .Setup(x => x.PermaDeleteAsync(lentItemId))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.PermaDeleteAsync(lentItemId);

            // Assert
            Assert.True(result);
            _mockLentItemsRepository.Verify(x => x.PermaDeleteAsync(lentItemId), Times.Once);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
