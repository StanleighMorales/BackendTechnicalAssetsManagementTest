using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class ArchiveItemsRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ArchiveItemsRepository _repository;

        public ArchiveItemsRepositoryTests()
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
            _context.Database.EnsureDeleted();
            _context.Dispose();
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
        public async Task GetAllItemArchivesAsync_WithData_ShouldReturnAllArchivedItems()
        {
            // Arrange
            var archivedItems = new List<ArchiveItems>
            {
                new ArchiveItems
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Archived Laptop",
                    SerialNumber = "ARCH001",
                    Barcode = "ARCH001",
                    ItemMake = "Dell"
                },
                new ArchiveItems
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Archived Mouse",
                    SerialNumber = "ARCH002",
                    Barcode = "ARCH002",
                    ItemMake = "Logitech"
                }
            };

            await _context.ArchiveItems.AddRangeAsync(archivedItems);
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
        public async Task GetItemArchiveByIdAsync_WithValidId_ShouldReturnArchivedItem()
        {
            // Arrange
            var archivedItem = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Archived Keyboard",
                SerialNumber = "ARCH003",
                Barcode = "ARCH003",
                ItemMake = "Microsoft"
            };

            await _context.ArchiveItems.AddAsync(archivedItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetItemArchiveByIdAsync(archivedItem.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archivedItem.Id, result.Id);
            Assert.Equal("Archived Keyboard", result.ItemName);
        }

        [Fact]
        public async Task GetItemArchiveByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act
            var result = await _repository.GetItemArchiveByIdAsync(invalidId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateItemArchiveAsync Tests

        [Fact]
        public async Task CreateItemArchiveAsync_WithValidArchivedItem_ShouldAddToContext()
        {
            // Arrange
            var archivedItem = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "New Archived Item",
                SerialNumber = "ARCH004",
                Barcode = "ARCH004",
                ItemMake = "HP"
            };

            // Act
            await _repository.CreateItemArchiveAsync(archivedItem);
            await _context.SaveChangesAsync();

            // Assert
            var result = await _context.ArchiveItems.FindAsync(archivedItem.Id);
            Assert.NotNull(result);
            Assert.Equal("New Archived Item", result.ItemName);
        }

        #endregion

        #region DeleteItemArchiveAsync Tests

        [Fact]
        public async Task DeleteItemArchiveAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var archivedItem = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "To Delete",
                SerialNumber = "ARCH005",
                Barcode = "ARCH005",
                ItemMake = "Asus"
            };

            await _context.ArchiveItems.AddAsync(archivedItem);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteItemArchiveAsync(archivedItem.Id);
            await _context.SaveChangesAsync();

            // Assert
            var result = await _context.ArchiveItems.FindAsync(archivedItem.Id);
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteItemArchiveAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            var invalidId = Guid.NewGuid();

            // Act & Assert
            await _repository.DeleteItemArchiveAsync(invalidId);
            // Should not throw exception
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldReturnTrue()
        {
            // Arrange
            var archivedItem = new ArchiveItems
            {
                Id = Guid.NewGuid(),
                ItemName = "Test Item",
                SerialNumber = "ARCH006",
                Barcode = "ARCH006",
                ItemMake = "Lenovo"
            };

            await _context.ArchiveItems.AddAsync(archivedItem);

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
