using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using BackendTechnicalAssetsManagement.src.IRepository;
using Microsoft.EntityFrameworkCore;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class ArchiveLentItemsRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<ILentItemsRepository> _mockLentItemsRepository;
        private readonly ArchiveLentItemsRepository _repository;

        public ArchiveLentItemsRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            _repository = new ArchiveLentItemsRepository(_context, _mockLentItemsRepository.Object);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetAllArchiveLentItemsAsync Tests

        [Fact]
        public async Task GetAllArchiveLentItemsAsync_WithNoData_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetAllArchiveLentItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllArchiveLentItemsAsync_WithData_ShouldReturnAllArchives()
        {
            // Arrange
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                SerialNumber = "SN-001",
                Barcode = "ITEM-SN-001",
                Status = ItemStatus.Available
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Mouse",
                SerialNumber = "SN-002",
                Barcode = "ITEM-SN-002",
                Status = ItemStatus.Available
            };

            await _context.Items.AddRangeAsync(new[] { item1, item2 });
            await _context.SaveChangesAsync();

            var archive1 = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ItemId = item1.Id,
                Status = "Returned",
                Barcode = "LENT-ARCH-001",
                ItemName = "Laptop",
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                Room = "101",
                SubjectTimeSchedule = "Math 9:00 AM",
                CreatedAt = DateTime.UtcNow
            };

            var archive2 = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ItemId = item2.Id,
                Status = "Returned",
                Barcode = "LENT-ARCH-002",
                ItemName = "Mouse",
                BorrowerFullName = "Jane Smith",
                BorrowerRole = "Teacher",
                Room = "102",
                SubjectTimeSchedule = "Science 10:00 AM",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveLentItems.AddRangeAsync(new[] { archive1, archive2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllArchiveLentItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetArchiveLentItemsByIdAsync Tests

        [Fact]
        public async Task GetArchiveLentItemsByIdAsync_WithValidId_ShouldReturnArchive()
        {
            // Arrange
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Keyboard",
                SerialNumber = "SN-003",
                Barcode = "ITEM-SN-003",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                UserId = Guid.NewGuid(),
                ItemId = item.Id,
                Status = "Returned",
                Barcode = "LENT-ARCH-003",
                ItemName = "Keyboard",
                BorrowerFullName = "Test User",
                BorrowerRole = "Student",
                Room = "103",
                SubjectTimeSchedule = "English 11:00 AM",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveLentItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetArchiveLentItemsByIdAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveId, result.Id);
            Assert.Equal("LENT-ARCH-003", result.Barcode);
        }

        [Fact]
        public async Task GetArchiveLentItemsByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetArchiveLentItemsByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateArchiveLentItemsAsync Tests

        [Fact]
        public async Task CreateArchiveLentItemsAsync_WithValidArchive_ShouldAddToContext()
        {
            // Arrange
            var archive = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-ARCH-004",
                ItemName = "Monitor",
                BorrowerFullName = "New User",
                BorrowerRole = "Student",
                Room = "104",
                SubjectTimeSchedule = "History 1:00 PM",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.CreateArchiveLentItemsAsync(archive);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archive.Id, result.Id);
            
            var savedArchive = await _context.ArchiveLentItems.FindAsync(archive.Id);
            Assert.NotNull(savedArchive);
            Assert.Equal("LENT-ARCH-004", savedArchive.Barcode);
        }

        #endregion

        #region UpdateArchiveLentItemsAsync Tests

        [Fact]
        public async Task UpdateArchiveLentItemsAsync_WithValidArchive_ShouldUpdateInContext()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-ARCH-005",
                ItemName = "Projector",
                BorrowerFullName = "Update User",
                BorrowerRole = "Teacher",
                Room = "105",
                SubjectTimeSchedule = "Physics 2:00 PM",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveLentItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Detach the entity to avoid tracking conflict
            _context.Entry(archive).State = EntityState.Detached;

            // Modify the archive
            var updatedArchive = new ArchiveLentItems
            {
                Id = archiveId,
                UserId = archive.UserId,
                ItemId = archive.ItemId,
                Status = "Updated Status",
                Barcode = "LENT-ARCH-005",
                ItemName = archive.ItemName,
                BorrowerFullName = archive.BorrowerFullName,
                BorrowerRole = archive.BorrowerRole,
                Room = archive.Room,
                SubjectTimeSchedule = archive.SubjectTimeSchedule,
                CreatedAt = archive.CreatedAt
            };

            // Act
            var result = await _repository.UpdateArchiveLentItemsAsync(archiveId, updatedArchive);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            var savedArchive = await _context.ArchiveLentItems.FindAsync(archiveId);
            Assert.NotNull(savedArchive);
            Assert.Equal("Updated Status", savedArchive.Status);
        }

        [Fact]
        public async Task UpdateArchiveLentItemsAsync_WithNonExistentArchive_ShouldReturnArchive()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updatedArchive = new ArchiveLentItems
            {
                Id = nonExistentId,
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Updated",
                Barcode = "LENT-NONE",
                ItemName = "None",
                BorrowerFullName = "None",
                BorrowerRole = "Student",
                Room = "000",
                SubjectTimeSchedule = "None",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.UpdateArchiveLentItemsAsync(nonExistentId, updatedArchive);

            // Assert - Update returns the archive even if it doesn't exist in DB yet
            Assert.NotNull(result);
            Assert.Equal(nonExistentId, result.Id);
        }

        #endregion

        #region DeleteArchiveLentItemsAsync Tests

        [Fact]
        public async Task DeleteArchiveLentItemsAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveLentItems
            {
                Id = archiveId,
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-ARCH-006",
                ItemName = "Tablet",
                BorrowerFullName = "Delete User",
                BorrowerRole = "Student",
                Room = "106",
                SubjectTimeSchedule = "Chemistry 3:00 PM",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveLentItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteArchiveLentItemsAsync(archiveId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedArchive = await _context.ArchiveLentItems.FindAsync(archiveId);
            Assert.Null(deletedArchive);
        }

        [Fact]
        public async Task DeleteArchiveLentItemsAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.DeleteArchiveLentItemsAsync(nonExistentId);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldReturnTrue()
        {
            // Arrange
            var archive = new ArchiveLentItems
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                Status = "Returned",
                Barcode = "LENT-ARCH-007",
                ItemName = "Headphones",
                BorrowerFullName = "Save User",
                BorrowerRole = "Student",
                Room = "107",
                SubjectTimeSchedule = "Music 4:00 PM",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveLentItems.AddAsync(archive);

            // Act
            var result = await _repository.SaveChangesAsync();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task SaveChangesAsync_WithNoChanges_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.SaveChangesAsync();

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
