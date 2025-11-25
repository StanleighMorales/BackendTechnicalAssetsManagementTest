using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.LentItems;
using BackendTechnicalAssetsManagement.src.DTOs.LentItems;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Microsoft.EntityFrameworkCore;
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
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IBarcodeGeneratorService> _mockBarcodeGenerator;
        private readonly LentItemsService _lentItemsService;

        // PERFORMANCE OPTIMIZATION: Share a single DbContext across all tests
        // Creating a new DbContext for each test is slow (~50ms per test)
        // Sharing one reduces total test time from ~5s to ~1s for 100 tests
        private static readonly Lazy<BackendTechnicalAssetsManagement.src.Data.AppDbContext> _sharedDbContext = new Lazy<BackendTechnicalAssetsManagement.src.Data.AppDbContext>(() =>
        {
            // CRITICAL: Set performance flags BEFORE creating DbContext
            BackendTechnicalAssetsManagement.src.Data.ModelBuilderExtensions.SkipSeedData = true;
            BackendTechnicalAssetsManagement.src.Utils.BarcodeImageUtil.SkipImageGeneration = true;

            var options = new DbContextOptionsBuilder<BackendTechnicalAssetsManagement.src.Data.AppDbContext>()
                .UseInMemoryDatabase(databaseName: "SharedTestDb")
                .EnableSensitiveDataLogging(false)
                .Options;

            return new BackendTechnicalAssetsManagement.src.Data.AppDbContext(options);
        });

        public LentItemsServiceTests()
        {

            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockArchiveLentItemsService = new Mock<IArchiveLentItemsService>();
            _mockUserService = new Mock<IUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockBarcodeGenerator = new Mock<IBarcodeGeneratorService>();

            // Use the shared DbContext for all tests
            _mockLentItemsRepository
                .Setup(x => x.GetDbContext())
                .Returns(_sharedDbContext.Value);

            // Setup default barcode generation mock
            _mockBarcodeGenerator
                .Setup(x => x.GenerateLentItemBarcodeAsync(It.IsAny<DateTime?>()))
                .ReturnsAsync("LENT-20251125-001");

            // Initialize the service with all mocks
            _lentItemsService = new LentItemsService(
                _mockLentItemsRepository.Object,
                _mockMapper.Object,
                _mockUserRepository.Object,
                _mockItemRepository.Object,
                _mockArchiveLentItemsService.Object,
                _mockUserService.Object,
                _mockBarcodeGenerator.Object
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
        public async Task AddAsync_WithBorrowedItem_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Borrowed Laptop",
                Status = ItemStatus.Borrowed,
                Condition = ItemCondition.Good
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddAsync(createDto));
            Assert.Contains("already borrowed", exception.Message, StringComparison.OrdinalIgnoreCase);
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

            _mockUserService
                .Setup(x => x.ValidateStudentProfileComplete(userId))
                .ReturnsAsync((true, string.Empty));

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
        public async Task ArchiveLentItems_WithNotReturnedItem_ShouldSetItemArchived()
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
            Assert.Equal(ItemStatus.Archived, item.Status);
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

        #region AddForGuestAsync Tests

        [Fact]
        public async Task AddForGuestAsync_WithValidData_ShouldCreateGuestLentItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemsForGuestDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Projector",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };
            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                ItemName = "Test Projector",
                BorrowerFullName = $"{createDto.BorrowerFirstName} {createDto.BorrowerLastName}",
                BorrowerRole = createDto.BorrowerRole,
                TeacherFullName = $"{createDto.TeacherFirstName} {createDto.TeacherLastName}",
                StudentIdNumber = createDto.StudentIdNumber,
                UserId = null,
                TeacherId = null,
                Status = "Borrowed",
                Barcode = "LENT-GUEST-001"
            };

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(lentItem);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems>());

            _mockLentItemsRepository
                .Setup(x => x.AddAsync(It.IsAny<LentItems>()))
                .ReturnsAsync(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto>(lentItem))
                .Returns(new LentItemsDto { Id = lentItem.Id, BorrowerFullName = lentItem.BorrowerFullName });

            // Act
            var result = await _lentItemsService.AddForGuestAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockLentItemsRepository.Verify(x => x.AddAsync(It.IsAny<LentItems>()), Times.Once);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddForGuestAsync_WithDefectiveItem_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemsForGuestDto(itemId);
            var item = new Item
            {
                Id = itemId,
                ItemName = "Broken Projector",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Defective
            };

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(new LentItems());

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems>());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddForGuestAsync(createDto));
            Assert.Contains("Defective", exception.Message);
            Assert.Contains("cannot be lent", exception.Message);
        }

        [Fact]
        public async Task AddForGuestAsync_WithBorrowingLimitReached_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemsForGuestDto(itemId);
            createDto.StudentIdNumber = "GUEST001";
            
            var existingLentItems = new List<LentItems>
            {
                new LentItems { StudentIdNumber = "GUEST001", Status = "Borrowed" },
                new LentItems { StudentIdNumber = "GUEST001", Status = "Pending" },
                new LentItems { StudentIdNumber = "GUEST001", Status = "Approved" }
            };

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(new LentItems());

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(existingLentItems);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddForGuestAsync(createDto));
            Assert.Contains("Borrowing limit reached", exception.Message);
            Assert.Contains("maximum of 3", exception.Message);
        }

        #endregion

        #region GetByBarcodeAsync Tests

        [Fact]
        public async Task GetByBarcodeAsync_WithValidBarcode_ShouldReturnLentItem()
        {
            // Arrange
            var barcode = "LENT-20251101-001";
            var lentItem = LentItemsMockData.GetMockLentItem();
            lentItem.Barcode = barcode;
            var lentItemDto = LentItemsMockData.GetMockLentItemsDto(lentItem.Id);

            _mockLentItemsRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto?>(lentItem))
                .Returns(lentItemDto);

            // Act
            var result = await _lentItemsService.GetByBarcodeAsync(barcode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lentItem.Id, result.Id);
            _mockLentItemsRepository.Verify(x => x.GetByBarcodeAsync(barcode), Times.Once);
        }

        [Fact]
        public async Task GetByBarcodeAsync_WithInvalidBarcode_ShouldReturnNull()
        {
            // Arrange
            var barcode = "INVALID-BARCODE";

            _mockLentItemsRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync((LentItems?)null);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto?>(It.IsAny<LentItems>()))
                .Returns((LentItemsDto?)null);

            // Act
            var result = await _lentItemsService.GetByBarcodeAsync(barcode);

            // Assert
            Assert.Null(result);
            _mockLentItemsRepository.Verify(x => x.GetByBarcodeAsync(barcode), Times.Once);
        }

        #endregion

        #region GetByDateTimeAsync Tests

        [Fact]
        public async Task GetByDateTimeAsync_WithValidDateTime_ShouldReturnLentItems()
        {
            // Arrange
            var dateTime = DateTime.UtcNow;
            var lentItems = LentItemsMockData.GetMockLentItemsList();
            var lentItemDtos = LentItemsMockData.GetMockLentItemsDtoList();

            _mockLentItemsRepository
                .Setup(x => x.GetByDateTime(dateTime))
                .ReturnsAsync(lentItems);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<LentItemsDto>>(lentItems))
                .Returns(lentItemDtos);

            // Act
            var result = await _lentItemsService.GetByDateTimeAsync(dateTime);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            _mockLentItemsRepository.Verify(x => x.GetByDateTime(dateTime), Times.Once);
        }

        [Fact]
        public async Task GetByDateTimeAsync_WithNoResults_ShouldReturnEmptyList()
        {
            // Arrange
            var dateTime = DateTime.UtcNow;

            _mockLentItemsRepository
                .Setup(x => x.GetByDateTime(dateTime))
                .ReturnsAsync(new List<LentItems>());

            _mockMapper
                .Setup(x => x.Map<IEnumerable<LentItemsDto>>(It.IsAny<IEnumerable<LentItems>>()))
                .Returns(new List<LentItemsDto>());

            // Act
            var result = await _lentItemsService.GetByDateTimeAsync(dateTime);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UpdateStatusByBarcodeAsync Tests

        [Fact]
        public async Task UpdateStatusByBarcodeAsync_WithValidBarcode_ShouldUpdateStatus()
        {
            // Arrange
            var barcode = "LENT-20251101-999";
            var lentItemId = Guid.NewGuid();
            var lentItem = LentItemsMockData.GetMockLentItem(lentItemId, "Borrowed");
            lentItem.Barcode = barcode;
            var item = new Item
            {
                Id = lentItem.ItemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Borrowed,
                Condition = ItemCondition.Good
            };
            var scanDto = LentItemsMockData.GetValidScanLentItemDto(LentItemsStatus.Returned);

            _mockLentItemsRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync(lentItem);

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
            var result = await _lentItemsService.UpdateStatusByBarcodeAsync(barcode, scanDto);

            // Assert
            Assert.True(result);
            _mockLentItemsRepository.Verify(x => x.GetByBarcodeAsync(barcode), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusByBarcodeAsync_WithInvalidBarcode_ShouldReturnFalse()
        {
            // Arrange
            var barcode = "INVALID-BARCODE";
            var scanDto = LentItemsMockData.GetValidScanLentItemDto();

            _mockLentItemsRepository
                .Setup(x => x.GetByBarcodeAsync(barcode))
                .ReturnsAsync((LentItems?)null);

            // Act
            var result = await _lentItemsService.UpdateStatusByBarcodeAsync(barcode, scanDto);

            // Assert
            Assert.False(result);
            _mockLentItemsRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        #endregion

        #region CancelExpiredReservationsAsync Tests

        [Fact]
        public async Task CancelExpiredReservationsAsync_WithExpiredReservations_ShouldCancelThem()
        {
            // Arrange
            var expiredReservation1 = LentItemsMockData.GetMockPendingLentItem();
            expiredReservation1.Status = "Approved";
            expiredReservation1.ReservedFor = DateTime.Now.AddHours(-3); // Expired 3 hours ago
            expiredReservation1.LentAt = null;

            var expiredReservation2 = LentItemsMockData.GetMockPendingLentItem();
            expiredReservation2.Status = "Pending";
            expiredReservation2.ReservedFor = DateTime.Now.AddHours(-2); // Expired 2 hours ago
            expiredReservation2.LentAt = null;

            var activeReservation = LentItemsMockData.GetMockPendingLentItem();
            activeReservation.Status = "Approved";
            activeReservation.ReservedFor = DateTime.Now.AddHours(1); // Future reservation
            activeReservation.LentAt = null;

            var allLentItems = new List<LentItems>
            {
                expiredReservation1,
                expiredReservation2,
                activeReservation
            };

            var item1 = new Item { Id = expiredReservation1.ItemId, Status = ItemStatus.Reserved };
            var item2 = new Item { Id = expiredReservation2.ItemId, Status = ItemStatus.Reserved };

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(allLentItems);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(expiredReservation1.ItemId))
                .ReturnsAsync(item1);

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(expiredReservation2.ItemId))
                .ReturnsAsync(item2);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.UpdateAsync(It.IsAny<LentItems>()))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.CancelExpiredReservationsAsync();

            // Assert
            Assert.Equal(2, result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(It.IsAny<LentItems>()), Times.Exactly(2));
            _mockItemRepository.Verify(x => x.UpdateAsync(It.IsAny<Item>()), Times.Exactly(2));
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CancelExpiredReservationsAsync_WithNoExpiredReservations_ShouldReturnZero()
        {
            // Arrange
            var activeReservation = LentItemsMockData.GetMockPendingLentItem();
            activeReservation.Status = "Approved";
            activeReservation.ReservedFor = DateTime.Now.AddHours(2); // Future reservation
            activeReservation.LentAt = null;

            var allLentItems = new List<LentItems> { activeReservation };

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(allLentItems);

            // Act
            var result = await _lentItemsService.CancelExpiredReservationsAsync();

            // Assert
            Assert.Equal(0, result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(It.IsAny<LentItems>()), Times.Never);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CancelExpiredReservationsAsync_WithAlreadyPickedUpReservation_ShouldNotCancel()
        {
            // Arrange
            var pickedUpReservation = LentItemsMockData.GetMockLentItem();
            pickedUpReservation.Status = "Borrowed";
            pickedUpReservation.ReservedFor = DateTime.Now.AddHours(-3); // Expired but picked up
            pickedUpReservation.LentAt = DateTime.Now.AddHours(-2); // Was picked up

            var allLentItems = new List<LentItems> { pickedUpReservation };

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(allLentItems);

            // Act
            var result = await _lentItemsService.CancelExpiredReservationsAsync();

            // Assert
            Assert.Equal(0, result);
            _mockLentItemsRepository.Verify(x => x.UpdateAsync(It.IsAny<LentItems>()), Times.Never);
        }

        #endregion

        #region IsItemAvailableForReservation Tests (via AddAsync)

        [Fact]
        public async Task AddAsync_WithNoReservedForTime_ShouldSkipTimeSlotValidation()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId, userId);
            createDto.ReservedFor = null; // No reservation time specified

            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };

            var user = new User
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "[email]",
                UserRole = UserRole.Student
            };

            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                Status = "Pending",
                Barcode = "LENT-001"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems>());

            _mockUserService
                .Setup(x => x.ValidateStudentProfileComplete(userId))
                .ReturnsAsync((true, string.Empty));

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.AddAsync(It.IsAny<LentItems>()))
                .ReturnsAsync(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItem.Id))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto>(lentItem))
                .Returns(new LentItemsDto { Id = lentItem.Id });

            // Act
            var result = await _lentItemsService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockLentItemsRepository.Verify(x => x.AddAsync(It.IsAny<LentItems>()), Times.Once);
        }

        [Fact]
        public async Task AddAsync_WithConflictingReservation_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var reservationTime = DateTime.Now.AddHours(5);
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId, userId);
            createDto.ReservedFor = reservationTime;

            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };

            // Existing reservation within the 2-hour buffer window with "Reserved" status
            // Note: Using "Reserved" instead of "Approved" because "Approved" would trigger
            // the "active lent item" check first (line 66-72 in LentItemsService.cs)
            var conflictingReservation = new LentItems
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                ReservedFor = reservationTime.AddMinutes(30), // 30 minutes after requested time
                Status = "Reserved",
                UserId = Guid.NewGuid()
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems> { conflictingReservation });

            _mockUserService
                .Setup(x => x.ValidateStudentProfileComplete(userId))
                .ReturnsAsync((true, string.Empty));

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(new LentItems());

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _lentItemsService.AddAsync(createDto));
            Assert.Contains("already reserved", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AddAsync_WithNoConflictingReservations_ShouldSucceed()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var reservationTime = DateTime.Now.AddHours(10);
            var createDto = LentItemsMockData.GetValidCreateLentItemDto(itemId, userId);
            createDto.ReservedFor = reservationTime;
            createDto.Status = "Pending"; // Use Pending status for reservation test

            var item = new Item
            {
                Id = itemId,
                ItemName = "Test Laptop",
                Status = ItemStatus.Available,
                Condition = ItemCondition.Good
            };

            var user = new User
            {
                Id = userId,
                FirstName = "John",
                LastName = "Doe",
                Email = "[email]",
                UserRole = UserRole.Student
            };

            // Existing reservation outside the 2-hour buffer window (5 hours away)
            // Using "Reserved" status to avoid triggering the "active lent item" check
            var nonConflictingReservation = new LentItems
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                ReservedFor = reservationTime.AddHours(5), // 5 hours after requested time
                Status = "Reserved",
                UserId = Guid.NewGuid()
            };

            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                ItemId = itemId,
                UserId = userId,
                ReservedFor = reservationTime,
                Status = "Pending",
                Barcode = "LENT-002"
            };

            _mockItemRepository
                .Setup(x => x.GetByIdAsync(itemId))
                .ReturnsAsync(item);

            _mockItemRepository
                .Setup(x => x.UpdateAsync(It.IsAny<Item>()))
                .Returns(Task.CompletedTask);

            _mockLentItemsRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<LentItems> { nonConflictingReservation });

            _mockUserService
                .Setup(x => x.ValidateStudentProfileComplete(userId))
                .ReturnsAsync((true, string.Empty));

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockMapper
                .Setup(x => x.Map<LentItems>(createDto))
                .Returns(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.AddAsync(It.IsAny<LentItems>()))
                .ReturnsAsync(lentItem);

            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockLentItemsRepository
                .Setup(x => x.GetByIdAsync(lentItem.Id))
                .ReturnsAsync(lentItem);

            _mockMapper
                .Setup(x => x.Map<LentItemsDto>(lentItem))
                .Returns(new LentItemsDto { Id = lentItem.Id });

            // Act
            var result = await _lentItemsService.AddAsync(createDto);

            // Assert
            Assert.NotNull(result);
            _mockLentItemsRepository.Verify(x => x.AddAsync(It.IsAny<LentItems>()), Times.Once);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_ShouldCallRepositorySaveChanges()
        {
            // Arrange
            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _lentItemsService.SaveChangesAsync();

            // Assert
            Assert.True(result);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenFails_ShouldReturnFalse()
        {
            // Arrange
            _mockLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _lentItemsService.SaveChangesAsync();

            // Assert
            Assert.False(result);
            _mockLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
