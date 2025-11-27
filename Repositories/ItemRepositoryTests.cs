using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class ItemRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly ItemRepository _repository;

        public ItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _repository = new ItemRepository(_context);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithNoData_ShouldReturnEmptyList()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_WithData_ShouldReturnAllItems()
        {
            // Arrange
            var item1 = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                SerialNumber = "SN-001",
                Status = ItemStatus.Available
            };

            var item2 = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Projector",
                SerialNumber = "SN-002",
                Status = ItemStatus.Borrowed
            };

            await _context.Items.AddRangeAsync(new[] { item1, item2 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnItem()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                ItemName = "Camera",
                SerialNumber = "SN-003",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(itemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(itemId, result.Id);
            Assert.Equal("Camera", result.ItemName);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByBarcodeAsync Tests

        [Fact]
        public async Task GetByBarcodeAsync_WithValidBarcode_ShouldReturnItem()
        {
            // Arrange
            var barcode = "ITEM-SN-004";
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Tablet",
                SerialNumber = "SN-004",
                Barcode = barcode,
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByBarcodeAsync(barcode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(barcode, result.Barcode);
        }

        [Fact]
        public async Task GetByBarcodeAsync_WithInvalidBarcode_ShouldReturnNull()
        {
            // Arrange
            var nonExistentBarcode = "ITEM-NONEXISTENT";

            // Act
            var result = await _repository.GetByBarcodeAsync(nonExistentBarcode);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetBySerialNumberAsync Tests

        [Fact]
        public async Task GetBySerialNumberAsync_WithValidSerialNumber_ShouldReturnItem()
        {
            // Arrange
            var serialNumber = "SN-005";
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Monitor",
                SerialNumber = serialNumber,
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySerialNumberAsync(serialNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serialNumber, result.SerialNumber);
        }

        [Fact]
        public async Task GetBySerialNumberAsync_CaseInsensitive_ShouldReturnItem()
        {
            // Arrange
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Keyboard",
                SerialNumber = "SN-006",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetBySerialNumberAsync("sn-006");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("SN-006", result.SerialNumber);
        }

        [Fact]
        public async Task GetBySerialNumberAsync_WithInvalidSerialNumber_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetBySerialNumberAsync("NONEXISTENT");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidItem_ShouldAddToContext()
        {
            // Arrange
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Mouse",
                SerialNumber = "SN-007",
                Status = ItemStatus.Available
            };

            // Act
            var result = await _repository.AddAsync(item);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(item.Id, result.Id);
            
            var savedItem = await _context.Items.FindAsync(item.Id);
            Assert.NotNull(savedItem);
            Assert.Equal("Mouse", savedItem.ItemName);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidItem_ShouldUpdateInContext()
        {
            // Arrange
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Old Name",
                SerialNumber = "SN-008",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Modify the item
            item.ItemName = "Updated Name";
            item.Status = ItemStatus.Borrowed;

            // Act
            await _repository.UpdateAsync(item);
            await _context.SaveChangesAsync();

            // Assert
            var updatedItem = await _context.Items.FindAsync(item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal("Updated Name", updatedItem.ItemName);
            Assert.Equal(ItemStatus.Borrowed, updatedItem.Status);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var itemId = Guid.NewGuid();
            var item = new Item
            {
                Id = itemId,
                ItemName = "Delete Me",
                SerialNumber = "SN-009",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(itemId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedItem = await _context.Items.FindAsync(itemId);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.DeleteAsync(nonExistentId);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldReturnTrue()
        {
            // Arrange
            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Save Test",
                SerialNumber = "SN-010",
                Status = ItemStatus.Available
            };

            await _context.Items.AddAsync(item);

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

        #region AddRangeAsync Tests

        [Fact]
        public async Task AddRangeAsync_WithMultipleItems_ShouldAddAllToContext()
        {
            // Arrange
            var items = new[]
            {
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Item 1",
                    SerialNumber = "SN-011",
                    Status = ItemStatus.Available
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Item 2",
                    SerialNumber = "SN-012",
                    Status = ItemStatus.Available
                },
                new Item
                {
                    Id = Guid.NewGuid(),
                    ItemName = "Item 3",
                    SerialNumber = "SN-013",
                    Status = ItemStatus.Available
                }
            };

            // Act
            await _repository.AddRangeAsync(items);
            await _context.SaveChangesAsync();

            // Assert
            var allItems = await _context.Items.ToListAsync();
            Assert.Equal(3, allItems.Count);
        }

        #endregion
    }
}
