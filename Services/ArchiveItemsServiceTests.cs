using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Items;
using BackendTechnicalAssetsManagement.src.DTOs.Item;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class ArchiveItemsServiceTests
    {
        private readonly Mock<IArchiveItemRepository> _mockArchiveItemRepository;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ArchiveItemsService _archiveItemsService;

        public ArchiveItemsServiceTests()
        {
            _mockArchiveItemRepository = new Mock<IArchiveItemRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockMapper = new Mock<IMapper>();

            _archiveItemsService = new ArchiveItemsService(
                _mockArchiveItemRepository.Object,
                _mockItemRepository.Object,
                _mockMapper.Object
            );
        }

        #region CreateItemArchiveAsync Tests

        [Fact]
        public async Task CreateItemArchiveAsync_WithValidData_ShouldCreateArchive()
        {
            // Arrange
            var createDto = new CreateArchiveItemsDto
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell",
                Category = "Electronics",
                Condition = "Good",
                Status = "Available"
            };

            var archiveItem = new ArchiveItems
            {
                Id = createDto.Id,
                SerialNumber = createDto.SerialNumber,
                ItemName = createDto.ItemName,
                ItemType = createDto.ItemType,
                ItemMake = createDto.ItemMake,
                Category = ItemCategory.Electronics,
                Condition = ItemCondition.Good,
                Status = ItemStatus.Available,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var archiveDto = new ArchiveItemsDto
            {
                Id = archiveItem.Id,
                SerialNumber = archiveItem.SerialNumber,
                ItemName = archiveItem.ItemName,
                ItemType = archiveItem.ItemType,
                ItemMake = archiveItem.ItemMake
            };

            _mockMapper
                .Setup(x => x.Map<ArchiveItems>(createDto))
                .Returns(archiveItem);

            _mockArchiveItemRepository
                .Setup(x => x.CreateItemArchiveAsync(It.IsAny<ArchiveItems>()))
                .ReturnsAsync(archiveItem);

            _mockMapper
                .Setup(x => x.Map<ArchiveItemsDto>(It.IsAny<ArchiveItems>()))
                .Returns(archiveDto);

            // Act
            var result = await _archiveItemsService.CreateItemArchiveAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Id, result.Id);
            Assert.Equal(createDto.SerialNumber, result.SerialNumber);
            Assert.Equal(createDto.ItemName, result.ItemName);
            _mockArchiveItemRepository.Verify(x => x.CreateItemArchiveAsync(It.IsAny<ArchiveItems>()), Times.Once);
        }

        #endregion

        #region GetAllItemArchivesAsync Tests

        [Fact]
        public async Task GetAllItemArchivesAsync_ShouldReturnAllArchivedItems()
        {
            // Arrange
            var archivedItems = new List<ArchiveItems>
            {
                new ArchiveItems
                {
                    Id = Guid.NewGuid(),
                    SerialNumber = "SN-001",
                    ItemName = "Item 1",
                    ItemType = "Laptop",
                    ItemMake = "Dell"
                },
                new ArchiveItems
                {
                    Id = Guid.NewGuid(),
                    SerialNumber = "SN-002",
                    ItemName = "Item 2",
                    ItemType = "Monitor",
                    ItemMake = "HP"
                }
            };

            var archivedDtos = new List<ArchiveItemsDto>
            {
                new ArchiveItemsDto
                {
                    Id = archivedItems[0].Id,
                    SerialNumber = "SN-001",
                    ItemName = "Item 1"
                },
                new ArchiveItemsDto
                {
                    Id = archivedItems[1].Id,
                    SerialNumber = "SN-002",
                    ItemName = "Item 2"
                }
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetAllItemArchivesAsync())
                .ReturnsAsync(archivedItems);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ArchiveItemsDto>>(archivedItems))
                .Returns(archivedDtos);

            // Act
            var result = await _archiveItemsService.GetAllItemArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockArchiveItemRepository.Verify(x => x.GetAllItemArchivesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllItemArchivesAsync_WithEmptyArchive_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyList = new List<ArchiveItems>();
            var emptyDtoList = new List<ArchiveItemsDto>();

            _mockArchiveItemRepository
                .Setup(x => x.GetAllItemArchivesAsync())
                .ReturnsAsync(emptyList);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ArchiveItemsDto>>(emptyList))
                .Returns(emptyDtoList);

            // Act
            var result = await _archiveItemsService.GetAllItemArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockArchiveItemRepository.Verify(x => x.GetAllItemArchivesAsync(), Times.Once);
        }

        #endregion

        #region GetItemArchiveByIdAsync Tests

        [Fact]
        public async Task GetItemArchiveByIdAsync_WithValidId_ShouldReturnArchivedItem()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedItem = new ArchiveItems
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            var archiveDto = new ArchiveItemsDto
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(archivedItem);

            _mockMapper
                .Setup(x => x.Map<ArchiveItemsDto?>(archivedItem))
                .Returns(archiveDto);

            // Act
            var result = await _archiveItemsService.GetItemArchiveByIdAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveId, result.Id);
            Assert.Equal("SN-12345", result.SerialNumber);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
        }

        [Fact]
        public async Task GetItemArchiveByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync((ArchiveItems?)null);

            _mockMapper
                .Setup(x => x.Map<ArchiveItemsDto?>(It.IsAny<ArchiveItems>()))
                .Returns((ArchiveItemsDto?)null);

            // Act
            var result = await _archiveItemsService.GetItemArchiveByIdAsync(archiveId);

            // Assert
            Assert.Null(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
        }

        #endregion

        #region DeleteItemArchiveAsync Tests

        [Fact]
        public async Task DeleteItemArchiveAsync_WithValidId_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedItem = new ArchiveItems
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(archivedItem);

            _mockArchiveItemRepository
                .Setup(x => x.DeleteItemArchiveAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveItemsService.DeleteItemArchiveAsync(archiveId);

            // Assert
            Assert.True(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.DeleteItemArchiveAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteItemArchiveAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync((ArchiveItems?)null);

            // Act
            var result = await _archiveItemsService.DeleteItemArchiveAsync(archiveId);

            // Assert
            Assert.False(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.DeleteItemArchiveAsync(It.IsAny<Guid>()), Times.Never);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteItemArchiveAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedItem = new ArchiveItems
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(archivedItem);

            _mockArchiveItemRepository
                .Setup(x => x.DeleteItemArchiveAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveItemsService.DeleteItemArchiveAsync(archiveId);

            // Assert
            Assert.False(result);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region RestoreItemAsync Tests

        [Fact]
        public async Task RestoreItemAsync_WithValidArchiveId_ShouldRestoreItemAndDeleteArchive()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var originalItemId = Guid.NewGuid();
            
            var archivedItem = new ArchiveItems
            {
                Id = originalItemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell",
                Category = ItemCategory.Electronics,
                Condition = ItemCondition.Good,
                Status = ItemStatus.Unavailable
            };

            var restoredItem = new Item
            {
                Id = originalItemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell",
                Category = ItemCategory.Electronics,
                Condition = ItemCondition.Good,
                Status = ItemStatus.Available,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var itemDto = new ItemDto
            {
                Id = originalItemId,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                ItemType = "Laptop",
                ItemMake = "Dell"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(archivedItem);

            _mockMapper
                .Setup(x => x.Map<Item>(archivedItem))
                .Returns(restoredItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ReturnsAsync(restoredItem);

            _mockArchiveItemRepository
                .Setup(x => x.DeleteItemArchiveAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(itemDto);

            // Act
            var result = await _archiveItemsService.RestoreItemAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(originalItemId, result.Id);
            Assert.Equal("SN-12345", result.SerialNumber);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockItemRepository.Verify(x => x.AddAsync(It.Is<Item>(i => i.Status == ItemStatus.Available)), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.DeleteItemArchiveAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RestoreItemAsync_WithNonExistentArchiveId_ShouldReturnNull()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync((ArchiveItems?)null);

            // Act
            var result = await _archiveItemsService.RestoreItemAsync(archiveId);

            // Assert
            Assert.Null(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockItemRepository.Verify(x => x.AddAsync(It.IsAny<Item>()), Times.Never);
            _mockArchiveItemRepository.Verify(x => x.DeleteItemArchiveAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task RestoreItemAsync_ShouldSetStatusToAvailable()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedItem = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                Status = ItemStatus.Unavailable // Archived as unavailable
            };

            var restoredItem = new Item
            {
                Id = archivedItem.Id,
                SerialNumber = "SN-12345",
                ItemName = "Test Item",
                Status = ItemStatus.Available
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(archivedItem);

            _mockMapper
                .Setup(x => x.Map<Item>(archivedItem))
                .Returns(restoredItem);

            _mockItemRepository
                .Setup(x => x.AddAsync(It.IsAny<Item>()))
                .ReturnsAsync(restoredItem);

            _mockArchiveItemRepository
                .Setup(x => x.DeleteItemArchiveAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ItemDto>(It.IsAny<Item>()))
                .Returns(new ItemDto { Id = restoredItem.Id });

            // Act
            var result = await _archiveItemsService.RestoreItemAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            _mockItemRepository.Verify(x => x.AddAsync(It.Is<Item>(i => i.Status == ItemStatus.Available)), Times.Once);
        }

        #endregion

        #region UpdateItemArchiveAsync Tests

        [Fact]
        public async Task UpdateItemArchiveAsync_WithValidData_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var updateDto = new UpdateArchiveItemsDto
            {
                ItemName = "Updated Item Name",
                ItemType = "Updated Type",
                Description = "Updated Description"
            };

            var existingArchive = new ArchiveItems
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Old Name",
                ItemType = "Old Type"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(existingArchive);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingArchive))
                .Callback<UpdateArchiveItemsDto, ArchiveItems>((src, dest) =>
                {
                    dest.ItemName = src.ItemName ?? dest.ItemName;
                    dest.ItemType = src.ItemType ?? dest.ItemType;
                    dest.Description = src.Description;
                });

            _mockArchiveItemRepository
                .Setup(x => x.UpdateItemArchiveAsync(It.IsAny<ArchiveItems>()))
                .ReturnsAsync(existingArchive);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveItemsService.UpdateItemArchiveAsync(archiveId, updateDto);

            // Assert
            Assert.True(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.UpdateItemArchiveAsync(existingArchive), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateItemArchiveAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var updateDto = new UpdateArchiveItemsDto
            {
                ItemName = "Updated Name"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync((ArchiveItems?)null);

            // Act
            var result = await _archiveItemsService.UpdateItemArchiveAsync(archiveId, updateDto);

            // Assert
            Assert.False(result);
            _mockArchiveItemRepository.Verify(x => x.GetItemArchiveByIdAsync(archiveId), Times.Once);
            _mockArchiveItemRepository.Verify(x => x.UpdateItemArchiveAsync(It.IsAny<ArchiveItems>()), Times.Never);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateItemArchiveAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var updateDto = new UpdateArchiveItemsDto
            {
                ItemName = "Updated Name"
            };

            var existingArchive = new ArchiveItems
            {
                Id = archiveId,
                SerialNumber = "SN-12345",
                ItemName = "Old Name"
            };

            _mockArchiveItemRepository
                .Setup(x => x.GetItemArchiveByIdAsync(archiveId))
                .ReturnsAsync(existingArchive);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingArchive))
                .Returns(existingArchive);

            _mockArchiveItemRepository
                .Setup(x => x.UpdateItemArchiveAsync(It.IsAny<ArchiveItems>()))
                .ReturnsAsync(existingArchive);

            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveItemsService.UpdateItemArchiveAsync(archiveId, updateDto);

            // Assert
            Assert.False(result);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WhenSuccessful_ShouldReturnTrue()
        {
            // Arrange
            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveItemsService.SaveChangesAsync();

            // Assert
            Assert.True(result);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenFails_ShouldReturnFalse()
        {
            // Arrange
            _mockArchiveItemRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveItemsService.SaveChangesAsync();

            // Assert
            Assert.False(result);
            _mockArchiveItemRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
