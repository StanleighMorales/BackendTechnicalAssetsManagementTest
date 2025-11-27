using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class ArchiveItemRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ArchiveItemsRepository _repository;

        public ArchiveItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _repository = new ArchiveItemsRepository(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetAllItemArchivesAsync Tests

        [Fact]
        public async Task GetAllItemArchivesAsync_WithNoData_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetAllItemArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllItemArchivesAsync_WithData_ShouldReturnAllArchives()
        {
            // Arrange
            var archive1 = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Archived Laptop",
                SerialNumber = "SN-ARCH-001",
                Barcode = "ITEM-SN-ARCH-001",
                ItemType = "Electronics",
                ItemMake = "Dell",
                CreatedAt = DateTime.UtcNow
            };

            var archive2 = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Archived Mouse",
                SerialNumber = "SN-ARCH-002",
                Barcode = "ITEM-SN-ARCH-002",
                ItemType = "Peripherals",
                ItemMake = "Logitech",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveItems.AddRangeAsync(new[] { archive1, archive2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllItemArchivesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetItemArchiveByIdAsync Tests

        [Fact]
        public async Task GetItemArchiveByIdAsync_WithValidId_ShouldReturnArchive()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveItems
            {
                Id = archiveId,
                ItemName = "Archived Keyboard",
                SerialNumber = "SN-ARCH-003",
                Barcode = "ITEM-SN-ARCH-003",
                ItemType = "Peripherals",
                ItemMake = "Microsoft",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetItemArchiveByIdAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveId, result.Id);
            Assert.Equal("Archived Keyboard", result.ItemName);
        }

        [Fact]
        public async Task GetItemArchiveByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetItemArchiveByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateItemArchiveAsync Tests

        [Fact]
        public async Task CreateItemArchiveAsync_WithValidArchive_ShouldAddToContext()
        {
            // Arrange
            var archive = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "New Archive",
                SerialNumber = "SN-ARCH-004",
                Barcode = "ITEM-SN-ARCH-004",
                ItemType = "Electronics",
                ItemMake = "HP",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.CreateItemArchiveAsync(archive);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archive.Id, result.Id);
            
            var savedArchive = await _context.ArchiveItems.FindAsync(archive.Id);
            Assert.NotNull(savedArchive);
            Assert.Equal("New Archive", savedArchive.ItemName);
        }

        #endregion

        #region UpdateItemArchiveAsync Tests

        [Fact]
        public async Task UpdateItemArchiveAsync_WithValidArchive_ShouldUpdateInContext()
        {
            // Arrange
            var archive = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Old Name",
                SerialNumber = "SN-ARCH-005",
                Barcode = "ITEM-SN-ARCH-005",
                ItemType = "Electronics",
                ItemMake = "Lenovo",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Modify the archive
            archive.ItemName = "Updated Name";

            // Act
            var result = await _repository.UpdateItemArchiveAsync(archive);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            var updatedArchive = await _context.ArchiveItems.FindAsync(archive.Id);
            Assert.NotNull(updatedArchive);
            Assert.Equal("Updated Name", updatedArchive.ItemName);
        }

        [Fact]
        public async Task UpdateItemArchiveAsync_WithNonExistentArchive_ShouldReturnArchive()
        {
            // Arrange
            var nonExistentArchive = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Non Existent",
                SerialNumber = "SN-NONE",
                Barcode = "ITEM-SN-NONE",
                ItemType = "Electronics",
                ItemMake = "Unknown",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.UpdateItemArchiveAsync(nonExistentArchive);

            // Assert - Update returns the archive even if it doesn't exist in DB yet
            Assert.NotNull(result);
            Assert.Equal(nonExistentArchive.Id, result.Id);
        }

        #endregion

        #region DeleteItemArchiveAsync Tests

        [Fact]
        public async Task DeleteItemArchiveAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveItems
            {
                Id = archiveId,
                ItemName = "To Delete",
                SerialNumber = "SN-ARCH-006",
                Barcode = "ITEM-SN-ARCH-006",
                ItemType = "Electronics",
                ItemMake = "Acer",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveItems.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteItemArchiveAsync(archiveId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedArchive = await _context.ArchiveItems.FindAsync(archiveId);
            Assert.Null(deletedArchive);
        }

        [Fact]
        public async Task DeleteItemArchiveAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.DeleteItemArchiveAsync(nonExistentId);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldReturnTrue()
        {
            // Arrange
            var archive = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Save Test",
                SerialNumber = "SN-ARCH-007",
                Barcode = "ITEM-SN-ARCH-007",
                ItemType = "Electronics",
                ItemMake = "Samsung",
                CreatedAt = DateTime.UtcNow
            };

            await _context.ArchiveItems.AddAsync(archive);

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
