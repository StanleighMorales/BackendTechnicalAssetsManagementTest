using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class LentItemsRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly LentItemsRepository _repository;

        public LentItemsRepositoryTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // Disable seed data for performance
            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _repository = new LentItemsRepository(_context);
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
        public async Task GetAllAsync_WithData_ShouldReturnAllLentItems()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "student1",
                Email = "student@test.com",
                FirstName = "John",
                LastName = "Doe",
                UserRole = UserRole.Student
            };

            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Laptop",
                SerialNumber = "SN-001",
                Status = ItemStatus.Borrowed
            };

            var lentItem1 = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = "LENT-001"
            };

            var lentItem2 = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Returned",
                Barcode = "LENT-002"
            };

            await _context.Users.AddAsync(user);
            await _context.Items.AddAsync(item);
            await _context.LentItems.AddRangeAsync(new[] { lentItem1, lentItem2 });
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
        public async Task GetByIdAsync_WithValidId_ShouldReturnLentItem()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            
            // Create related entities first
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Test Item",
                SerialNumber = "SN-TEST",
                Status = ItemStatus.Borrowed
            };

            var lentItem = new LentItems
            {
                Id = lentItemId,
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = "LENT-003"
            };

            await _context.Users.AddAsync(user);
            await _context.Items.AddAsync(item);
            await _context.LentItems.AddAsync(lentItem);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(lentItemId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lentItemId, result.Id);
            Assert.Equal("Borrowed", result.Status);
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
        public async Task GetByBarcodeAsync_WithValidBarcode_ShouldReturnLentItem()
        {
            // Arrange
            var barcode = "LENT-20251127-001";
            
            // Create related entities first
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "barcodeuser",
                Email = "barcode@example.com",
                FirstName = "Barcode",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Barcode Item",
                SerialNumber = "SN-BARCODE",
                Status = ItemStatus.Borrowed
            };

            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = barcode
            };

            await _context.Users.AddAsync(user);
            await _context.Items.AddAsync(item);
            await _context.LentItems.AddAsync(lentItem);
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
            var nonExistentBarcode = "LENT-99999999-999";

            // Act
            var result = await _repository.GetByBarcodeAsync(nonExistentBarcode);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByDateTime Tests

        [Fact]
        public async Task GetByDateTime_WithDateOnly_ShouldReturnItemsOnThatDate()
        {
            // Arrange
            var targetDate = new DateTime(2025, 11, 27, 0, 0, 0, DateTimeKind.Utc);
            
            // Create related entities first
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "dateuser",
                Email = "date@example.com",
                FirstName = "Date",
                LastName = "User",
                UserRole = UserRole.Student
            };

            var item = new Item
            {
                Id = Guid.NewGuid(),
                ItemName = "Date Item",
                SerialNumber = "SN-DATE",
                Status = ItemStatus.Borrowed
            };

            var lentItem1 = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = "LENT-001",
                LentAt = new DateTime(2025, 11, 27, 10, 30, 0, DateTimeKind.Utc)
            };

            var lentItem2 = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = "LENT-002",
                LentAt = new DateTime(2025, 11, 27, 15, 45, 0, DateTimeKind.Utc)
            };

            var lentItem3 = new LentItems
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ItemId = item.Id,
                Status = "Borrowed",
                Barcode = "LENT-003",
                LentAt = new DateTime(2025, 11, 28, 10, 0, 0, DateTimeKind.Utc) // Different date
            };

            await _context.Users.AddAsync(user);
            await _context.Items.AddAsync(item);
            await _context.LentItems.AddRangeAsync(new[] { lentItem1, lentItem2, lentItem3 });
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByDateTime(targetDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, li => li.Barcode == "LENT-001");
            Assert.Contains(result, li => li.Barcode == "LENT-002");
            Assert.DoesNotContain(result, li => li.Barcode == "LENT-003");
        }

        [Fact]
        public async Task GetByDateTime_WithNoMatches_ShouldReturnEmptyList()
        {
            // Arrange
            var targetDate = new DateTime(2025, 12, 25, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var result = await _repository.GetByDateTime(targetDate);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidLentItem_ShouldAddToContext()
        {
            // Arrange
            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                Status = "Pending",
                Barcode = "LENT-NEW-001"
            };

            // Act
            var result = await _repository.AddAsync(lentItem);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(lentItem.Id, result.Id);
            
            var savedItem = await _context.LentItems.FindAsync(lentItem.Id);
            Assert.NotNull(savedItem);
            Assert.Equal("LENT-NEW-001", savedItem.Barcode);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidLentItem_ShouldUpdateInContext()
        {
            // Arrange
            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                Status = "Pending",
                Barcode = "LENT-UPDATE-001"
            };

            await _context.LentItems.AddAsync(lentItem);
            await _context.SaveChangesAsync();

            // Modify the item
            lentItem.Status = "Approved";

            // Act
            await _repository.UpdateAsync(lentItem);
            await _context.SaveChangesAsync();

            // Assert
            var updatedItem = await _context.LentItems.FindAsync(lentItem.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal("Approved", updatedItem.Status);
        }

        #endregion

        #region PermaDeleteAsync Tests

        [Fact]
        public async Task PermaDeleteAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var lentItemId = Guid.NewGuid();
            var lentItem = new LentItems
            {
                Id = lentItemId,
                Status = "Returned",
                Barcode = "LENT-DELETE-001"
            };

            await _context.LentItems.AddAsync(lentItem);
            await _context.SaveChangesAsync();

            // Act
            await _repository.PermaDeleteAsync(lentItemId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedItem = await _context.LentItems.FindAsync(lentItemId);
            Assert.Null(deletedItem);
        }

        [Fact]
        public async Task PermaDeleteAsync_WithInvalidId_ShouldNotThrow()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act & Assert - Should not throw
            await _repository.PermaDeleteAsync(nonExistentId);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region SaveChangesAsync Tests

        [Fact]
        public async Task SaveChangesAsync_WithChanges_ShouldReturnTrue()
        {
            // Arrange
            var lentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                Status = "Borrowed",
                Barcode = "LENT-SAVE-001"
            };

            await _context.LentItems.AddAsync(lentItem);

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

        #region GetDbContext Tests

        [Fact]
        public void GetDbContext_ShouldReturnContext()
        {
            // Act
            var result = _repository.GetDbContext();

            // Assert
            Assert.NotNull(result);
            Assert.Same(_context, result);
        }

        #endregion
    }
}
