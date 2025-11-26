using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.LentItems;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class ArchiveLentItemsServiceTests
    {
        private readonly Mock<IArchiveLentItemsRepository> _mockArchiveLentItemsRepository;
        private readonly Mock<ILentItemsRepository> _mockLentItemsRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ArchiveLentItemsService _archiveLentItemsService;

        public ArchiveLentItemsServiceTests()
        {
            _mockArchiveLentItemsRepository = new Mock<IArchiveLentItemsRepository>();
            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            _mockMapper = new Mock<IMapper>();

            _archiveLentItemsService = new ArchiveLentItemsService(
                _mockArchiveLentItemsRepository.Object,
                _mockLentItemsRepository.Object,
                _mockMapper.Object
            );
        }

        #region CreateLentItemsArchiveAsync Tests

        [Fact]
        public async Task CreateLentItemsArchiveAsync_WithValidData_ShouldCreateArchive()
        {
            // Arrange
            var createDto = new CreateArchiveLentItemsDto
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Laptop",
                UserId = Guid.NewGuid(),
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                StudentIdNumber = "2024-001",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10:00-11:00",
                LentAt = DateTime.UtcNow,
                Status = "Returned",
                Remarks = "In good condition",
                Barcode = "LENT-20241126-001"
            };

            var archiveLentItems = new ArchiveLentItems
            {
                Id = createDto.Id,
                ItemId = createDto.ItemId,
                ItemName = createDto.ItemName ?? string.Empty,
                UserId = createDto.UserId,
                BorrowerFullName = createDto.BorrowerFullName ?? string.Empty,
                BorrowerRole = createDto.BorrowerRole ?? string.Empty,
                StudentIdNumber = createDto.StudentIdNumber,
                Room = createDto.Room ?? string.Empty,
                SubjectTimeSchedule = createDto.SubjectTimeSchedule ?? string.Empty,
                LentAt = createDto.LentAt,
                Status = createDto.Status,
                Remarks = createDto.Remarks,
                Barcode = createDto.Barcode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var archiveDto = new ArchiveLentItemsDto
            {
                Id = archiveLentItems.Id,
                UserId = archiveLentItems.UserId,
                BorrowerFullName = archiveLentItems.BorrowerFullName,
                Status = archiveLentItems.Status
            };

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItems>(createDto))
                .Returns(archiveLentItems);

            _mockArchiveLentItemsRepository
                .Setup(x => x.CreateArchiveLentItemsAsync(It.IsAny<ArchiveLentItems>()))
                .ReturnsAsync(archiveLentItems);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto>(It.IsAny<ArchiveLentItems>()))
                .Returns(archiveDto);

            // Act
            var result = await _archiveLentItemsService.CreateLentItemsArchiveAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Id, result.Id);
            Assert.Equal(createDto.BorrowerFullName, result.BorrowerFullName);
            _mockArchiveLentItemsRepository.Verify(x => x.CreateArchiveLentItemsAsync(It.IsAny<ArchiveLentItems>()), Times.Once);
        }

        [Fact]
        public async Task CreateLentItemsArchiveAsync_ShouldSetTimestamps()
        {
            // Arrange
            var createDto = new CreateArchiveLentItemsDto
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Item",
                BorrowerFullName = "Jane Smith",
                Status = "Returned"
            };

            ArchiveLentItems? capturedArchive = null;
            var archiveLentItems = new ArchiveLentItems
            {
                Id = createDto.Id,
                ItemId = createDto.ItemId,
                ItemName = createDto.ItemName ?? string.Empty,
                BorrowerFullName = createDto.BorrowerFullName ?? string.Empty
            };

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItems>(createDto))
                .Returns(archiveLentItems);

            _mockArchiveLentItemsRepository
                .Setup(x => x.CreateArchiveLentItemsAsync(It.IsAny<ArchiveLentItems>()))
                .Callback<ArchiveLentItems>(archive => capturedArchive = archive)
                .ReturnsAsync(archiveLentItems);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto>(It.IsAny<ArchiveLentItems>()))
                .Returns(new ArchiveLentItemsDto { Id = createDto.Id });

            // Act
            await _archiveLentItemsService.CreateLentItemsArchiveAsync(createDto);

            // Assert
            Assert.NotNull(capturedArchive);
            Assert.NotEqual(default(DateTime), capturedArchive.CreatedAt);
            Assert.NotEqual(default(DateTime), capturedArchive.UpdatedAt);
            // Allow small time difference (within 1 second) due to execution timing
            var timeDifference = Math.Abs((capturedArchive.CreatedAt - capturedArchive.UpdatedAt).TotalSeconds);
            Assert.True(timeDifference < 1, "CreatedAt and UpdatedAt should be set to approximately the same time");
        }

        #endregion

        #region GetAllLentItemsArchivesAsync Tests

        [Fact]
        public async Task GetAllLentItemsArchivesAsync_ShouldReturnAllArchives()
        {
            // Arrange
            var archives = new List<ArchiveLentItems>
            {
                new ArchiveLentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Laptop 1",
                    BorrowerFullName = "John Doe",
                    Status = "Returned"
                },
                new ArchiveLentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Projector 1",
                    BorrowerFullName = "Jane Smith",
                    Status = "Returned"
                }
            };

            var archiveDtos = new List<ArchiveLentItemsDto>
            {
                new ArchiveLentItemsDto
                {
                    Id = archives[0].Id,
                    BorrowerFullName = "John Doe",
                    Status = "Returned"
                },
                new ArchiveLentItemsDto
                {
                    Id = archives[1].Id,
                    BorrowerFullName = "Jane Smith",
                    Status = "Returned"
                }
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetAllArchiveLentItemsAsync())
                .ReturnsAsync(archives);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ArchiveLentItemsDto>>(archives))
                .Returns(archiveDtos);

            // Act
            var result = await _archiveLentItemsService.GetAllLentItemsArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockArchiveLentItemsRepository.Verify(x => x.GetAllArchiveLentItemsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllLentItemsArchivesAsync_WithEmptyArchive_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyList = new List<ArchiveLentItems>();
            var emptyDtoList = new List<ArchiveLentItemsDto>();

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetAllArchiveLentItemsAsync())
                .ReturnsAsync(emptyList);

            _mockMapper
                .Setup(x => x.Map<IEnumerable<ArchiveLentItemsDto>>(emptyList))
                .Returns(emptyDtoList);

            // Act
            var result = await _archiveLentItemsService.GetAllLentItemsArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockArchiveLentItemsRepository.Verify(x => x.GetAllArchiveLentItemsAsync(), Times.Once);
        }

        #endregion

        #region GetLentItemsArchiveByIdAsync Tests

        [Fact]
        public async Task GetLentItemsArchiveByIdAsync_WithValidId_ShouldReturnArchive()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                ItemId = Guid.NewGuid(),
                ItemName = "Test Laptop",
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                StudentIdNumber = "2024-001",
                Room = "Room 101",
                Status = "Returned",
                Barcode = "LENT-20241126-001"
            };

            var archiveDto = new ArchiveLentItemsDto
            {
                Id = archiveId,
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                StudentIdNumber = "2024-001",
                Room = "Room 101",
                Status = "Returned"
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync(archive);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto?>(archive))
                .Returns(archiveDto);

            // Act
            var result = await _archiveLentItemsService.GetLentItemsArchiveByIdAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveId, result.Id);
            Assert.Equal("John Doe", result.BorrowerFullName);
            Assert.Equal("Student", result.BorrowerRole);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
        }

        [Fact]
        public async Task GetLentItemsArchiveByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync((ArchiveLentItems?)null);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto?>(It.IsAny<ArchiveLentItems>()))
                .Returns((ArchiveLentItemsDto?)null);

            // Act
            var result = await _archiveLentItemsService.GetLentItemsArchiveByIdAsync(archiveId);

            // Assert
            Assert.Null(result);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
        }

        #endregion

        #region DeleteLentItemsArchiveAsync Tests

        [Fact]
        public async Task DeleteLentItemsArchiveAsync_WithValidId_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                ItemId = Guid.NewGuid(),
                ItemName = "Test Item",
                BorrowerFullName = "John Doe",
                Status = "Returned"
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync(archive);

            _mockArchiveLentItemsRepository
                .Setup(x => x.DeleteArchiveLentItemsAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveLentItemsService.DeleteLentItemsArchiveAsync(archiveId);

            // Assert
            Assert.True(result);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
            _mockArchiveLentItemsRepository.Verify(x => x.DeleteArchiveLentItemsAsync(archiveId), Times.Once);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteLentItemsArchiveAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync((ArchiveLentItems?)null);

            // Act
            var result = await _archiveLentItemsService.DeleteLentItemsArchiveAsync(archiveId);

            // Assert
            Assert.False(result);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
            _mockArchiveLentItemsRepository.Verify(x => x.DeleteArchiveLentItemsAsync(It.IsAny<Guid>()), Times.Never);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteLentItemsArchiveAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                ItemId = Guid.NewGuid(),
                ItemName = "Test Item",
                BorrowerFullName = "John Doe",
                Status = "Returned"
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync(archive);

            _mockArchiveLentItemsRepository
                .Setup(x => x.DeleteArchiveLentItemsAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveLentItemsService.DeleteLentItemsArchiveAsync(archiveId);

            // Assert
            Assert.False(result);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region RestoreLentItemsAsync Tests

        [Fact]
        public async Task RestoreLentItemsAsync_WithValidArchiveId_ShouldRestoreAndDeleteArchive()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedLentItems = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Laptop",
                UserId = Guid.NewGuid(),
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                StudentIdNumber = "2024-001",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10:00-11:00",
                LentAt = DateTime.UtcNow.AddDays(-7),
                ReturnedAt = DateTime.UtcNow,
                Status = "Returned",
                Remarks = "Good condition",
                Barcode = "LENT-20241126-001"
            };

            var restoredLentItems = new LentItems
            {
                Id = archivedLentItems.Id,
                ItemId = archivedLentItems.ItemId,
                UserId = archivedLentItems.UserId,
                Room = archivedLentItems.Room,
                SubjectTimeSchedule = archivedLentItems.SubjectTimeSchedule,
                LentAt = archivedLentItems.LentAt,
                ReturnedAt = archivedLentItems.ReturnedAt,
                Status = archivedLentItems.Status,
                Remarks = archivedLentItems.Remarks,
                Barcode = archivedLentItems.Barcode
            };

            var archiveDto = new ArchiveLentItemsDto
            {
                Id = restoredLentItems.Id,
                UserId = restoredLentItems.UserId,
                BorrowerFullName = archivedLentItems.BorrowerFullName,
                Status = restoredLentItems.Status
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync(archivedLentItems);

            _mockMapper
                .Setup(x => x.Map<LentItems>(archivedLentItems))
                .Returns(restoredLentItems);

            _mockLentItemsRepository
                .Setup(x => x.AddAsync(It.IsAny<LentItems>()))
                .ReturnsAsync(restoredLentItems);

            _mockArchiveLentItemsRepository
                .Setup(x => x.DeleteArchiveLentItemsAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto>(It.IsAny<LentItems>()))
                .Returns(archiveDto);

            // Act
            var result = await _archiveLentItemsService.RestoreLentItemsAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archivedLentItems.Id, result.Id);
            Assert.Equal("John Doe", result.BorrowerFullName);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
            _mockLentItemsRepository.Verify(x => x.AddAsync(It.IsAny<LentItems>()), Times.Once);
            _mockArchiveLentItemsRepository.Verify(x => x.DeleteArchiveLentItemsAsync(archiveId), Times.Once);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RestoreLentItemsAsync_WithNonExistentArchiveId_ShouldReturnNull()
        {
            // Arrange
            var archiveId = Guid.NewGuid();

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync((ArchiveLentItems?)null);

            // Act
            var result = await _archiveLentItemsService.RestoreLentItemsAsync(archiveId);

            // Assert
            Assert.Null(result);
            _mockArchiveLentItemsRepository.Verify(x => x.GetArchiveLentItemsByIdAsync(archiveId), Times.Once);
            _mockLentItemsRepository.Verify(x => x.AddAsync(It.IsAny<LentItems>()), Times.Never);
            _mockArchiveLentItemsRepository.Verify(x => x.DeleteArchiveLentItemsAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task RestoreLentItemsAsync_ShouldPreserveAllData()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archivedLentItems = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Item",
                UserId = Guid.NewGuid(),
                TeacherId = Guid.NewGuid(),
                BorrowerFullName = "Jane Smith",
                BorrowerRole = "Teacher",
                TeacherFullName = "Jane Smith",
                Room = "Lab 202",
                SubjectTimeSchedule = "TTH 2:00-3:30",
                LentAt = DateTime.UtcNow.AddDays(-5),
                ReturnedAt = DateTime.UtcNow.AddDays(-1),
                Status = "Returned",
                Remarks = "Equipment in excellent condition",
                IsHiddenFromUser = false,
                Barcode = "LENT-20241126-002"
            };

            LentItems? capturedLentItems = null;
            var restoredLentItems = new LentItems
            {
                Id = archivedLentItems.Id,
                ItemId = archivedLentItems.ItemId,
                UserId = archivedLentItems.UserId,
                TeacherId = archivedLentItems.TeacherId,
                Room = archivedLentItems.Room,
                SubjectTimeSchedule = archivedLentItems.SubjectTimeSchedule,
                LentAt = archivedLentItems.LentAt,
                ReturnedAt = archivedLentItems.ReturnedAt,
                Status = archivedLentItems.Status,
                Remarks = archivedLentItems.Remarks,
                IsHiddenFromUser = archivedLentItems.IsHiddenFromUser,
                Barcode = archivedLentItems.Barcode
            };

            _mockArchiveLentItemsRepository
                .Setup(x => x.GetArchiveLentItemsByIdAsync(archiveId))
                .ReturnsAsync(archivedLentItems);

            _mockMapper
                .Setup(x => x.Map<LentItems>(archivedLentItems))
                .Returns(restoredLentItems);

            _mockLentItemsRepository
                .Setup(x => x.AddAsync(It.IsAny<LentItems>()))
                .Callback<LentItems>(item => capturedLentItems = item)
                .ReturnsAsync(restoredLentItems);

            _mockArchiveLentItemsRepository
                .Setup(x => x.DeleteArchiveLentItemsAsync(archiveId))
                .Returns(Task.CompletedTask);

            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockMapper
                .Setup(x => x.Map<ArchiveLentItemsDto>(It.IsAny<LentItems>()))
                .Returns(new ArchiveLentItemsDto { Id = restoredLentItems.Id });

            // Act
            var result = await _archiveLentItemsService.RestoreLentItemsAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(capturedLentItems);
            Assert.Equal(archivedLentItems.Id, capturedLentItems.Id);
            Assert.Equal(archivedLentItems.ItemId, capturedLentItems.ItemId);
            Assert.Equal(archivedLentItems.Room, capturedLentItems.Room);
            Assert.Equal(archivedLentItems.Status, capturedLentItems.Status);
            Assert.Equal(archivedLentItems.Barcode, capturedLentItems.Barcode);
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WhenSuccessful_ShouldReturnTrue()
        {
            // Arrange
            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveLentItemsService.SaveChangesAsync();

            // Assert
            Assert.True(result);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenFails_ShouldReturnFalse()
        {
            // Arrange
            _mockArchiveLentItemsRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveLentItemsService.SaveChangesAsync();

            // Assert
            Assert.False(result);
            _mockArchiveLentItemsRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
