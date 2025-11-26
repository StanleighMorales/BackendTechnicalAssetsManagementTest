using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class SummaryServiceTests
    {
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<ILentItemsRepository> _mockLentItemsRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly SummaryService _summaryService;

        public SummaryServiceTests()
        {
            _mockItemRepository = new Mock<IItemRepository>();
            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _summaryService = new SummaryService(
                _mockItemRepository.Object,
                _mockLentItemsRepository.Object,
                _mockUserRepository.Object
            );
        }

        #region GetOverallSummaryAsync Tests

        [Fact]
        public async Task GetOverallSummaryAsync_WithValidData_ShouldReturnCorrectSummary()
        {
            // Arrange
            var mockItems = ItemMockData.GetMockItemList();
            var mockLentItems = SummaryMockData.GetMockLentItemsList();
            var mockUsers = SummaryMockData.GetMockActiveUsersList();

            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockLentItems);
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetOverallSummaryAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.TotalItems); // From ItemMockData
            Assert.Equal(5, result.TotalLentItems); // From SummaryMockData
            Assert.Equal(15, result.TotalActiveUsers); // Online users only
            Assert.Equal(5, result.TotalItemsCategories); // All enum categories
            Assert.NotNull(result.ItemStocks);
            Assert.NotEmpty(result.ItemStocks);
        }

        [Fact]
        public async Task GetOverallSummaryAsync_WithEmptyDatabase_ShouldReturnZeroValues()
        {
            // Arrange
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Item>());
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            var result = await _summaryService.GetOverallSummaryAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(0, result.TotalLentItems);
            Assert.Equal(0, result.TotalActiveUsers);
            Assert.Equal(5, result.TotalItemsCategories); // Enum count remains constant
            Assert.NotNull(result.ItemStocks);
            Assert.Empty(result.ItemStocks);
        }

        [Fact]
        public async Task GetOverallSummaryAsync_ShouldCalculateStockCorrectly()
        {
            // Arrange
            var mockItems = new List<Item>
            {
                ItemMockData.GetMockItem(itemType: "Laptop", status: ItemStatus.Available),
                ItemMockData.GetMockItem(itemType: "Laptop", status: ItemStatus.Borrowed),
                ItemMockData.GetMockItem(itemType: "Laptop", status: ItemStatus.Available),
                ItemMockData.GetMockItem(itemType: "Mouse", status: ItemStatus.Available),
                ItemMockData.GetMockItem(itemType: "Mouse", status: ItemStatus.Borrowed)
            };

            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            var result = await _summaryService.GetOverallSummaryAsync();

            // Assert
            Assert.NotNull(result.ItemStocks);
            Assert.Equal(2, result.ItemStocks.Count); // 2 item types

            var laptopStock = result.ItemStocks.FirstOrDefault(s => s.ItemType == "Laptop");
            Assert.NotNull(laptopStock);
            Assert.Equal(3, laptopStock.TotalCount);
            Assert.Equal(2, laptopStock.AvailableCount);
            Assert.Equal(1, laptopStock.BorrowedCount);

            var mouseStock = result.ItemStocks.FirstOrDefault(s => s.ItemType == "Mouse");
            Assert.NotNull(mouseStock);
            Assert.Equal(2, mouseStock.TotalCount);
            Assert.Equal(1, mouseStock.AvailableCount);
            Assert.Equal(1, mouseStock.BorrowedCount);
        }

        [Fact]
        public async Task GetOverallSummaryAsync_ShouldOrderItemStocksByItemType()
        {
            // Arrange
            var mockItems = new List<Item>
            {
                ItemMockData.GetMockItem(itemType: "Zebra"),
                ItemMockData.GetMockItem(itemType: "Apple"),
                ItemMockData.GetMockItem(itemType: "Mouse")
            };

            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            var result = await _summaryService.GetOverallSummaryAsync();

            // Assert
            Assert.NotNull(result.ItemStocks);
            Assert.Equal(3, result.ItemStocks.Count);
            Assert.Equal("Apple", result.ItemStocks[0].ItemType);
            Assert.Equal("Mouse", result.ItemStocks[1].ItemType);
            Assert.Equal("Zebra", result.ItemStocks[2].ItemType);
        }

        #endregion

        #region GetItemCountAsync Tests

        [Fact]
        public async Task GetItemCountAsync_WithValidData_ShouldReturnCorrectCounts()
        {
            // Arrange
            var mockItems = ItemMockData.GetMockItemList();
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);

            // Act
            var result = await _summaryService.GetItemCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.TotalItems);
            
            // Condition counts
            Assert.Equal(3, result.NewItems); // New condition
            Assert.Equal(8, result.GoodItems); // Good condition
            Assert.Equal(1, result.DefectiveItems); // Defective condition
            Assert.Equal(2, result.RefurbishedItems); // Refurbished condition
            Assert.Equal(1, result.NeedRepairItems); // NeedRepair condition
            
            // Category counts
            Assert.Equal(5, result.Electronic); // Electronics category
            Assert.Equal(2, result.Keys); // Keys category
            Assert.Equal(3, result.MediaEquipment); // MediaEquipment category
            Assert.Equal(2, result.Tools); // Tools category
            Assert.Equal(3, result.Miscellaneous); // Miscellaneous category
        }

        [Fact]
        public async Task GetItemCountAsync_WithEmptyDatabase_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Item>());

            // Act
            var result = await _summaryService.GetItemCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalItems);
            Assert.Equal(0, result.NewItems);
            Assert.Equal(0, result.GoodItems);
            Assert.Equal(0, result.DefectiveItems);
            Assert.Equal(0, result.RefurbishedItems);
            Assert.Equal(0, result.NeedRepairItems);
            Assert.Equal(0, result.Electronic);
            Assert.Equal(0, result.Keys);
            Assert.Equal(0, result.MediaEquipment);
            Assert.Equal(0, result.Tools);
            Assert.Equal(0, result.Miscellaneous);
        }

        [Fact]
        public async Task GetItemCountAsync_WithOnlyNewItems_ShouldCountCorrectly()
        {
            // Arrange
            var mockItems = new List<Item>
            {
                ItemMockData.GetMockItem(condition: ItemCondition.New, category: ItemCategory.Electronics),
                ItemMockData.GetMockItem(condition: ItemCondition.New, category: ItemCategory.Keys),
                ItemMockData.GetMockItem(condition: ItemCondition.New, category: ItemCategory.Tools)
            };
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);

            // Act
            var result = await _summaryService.GetItemCountAsync();

            // Assert
            Assert.Equal(3, result.TotalItems);
            Assert.Equal(3, result.NewItems);
            Assert.Equal(0, result.GoodItems);
            Assert.Equal(0, result.DefectiveItems);
        }

        [Fact]
        public async Task GetItemCountAsync_WithMixedCategories_ShouldCountEachCategory()
        {
            // Arrange
            var mockItems = new List<Item>
            {
                ItemMockData.GetMockItem(category: ItemCategory.Electronics),
                ItemMockData.GetMockItem(category: ItemCategory.Electronics),
                ItemMockData.GetMockItem(category: ItemCategory.Keys),
                ItemMockData.GetMockItem(category: ItemCategory.MediaEquipment),
                ItemMockData.GetMockItem(category: ItemCategory.Tools),
                ItemMockData.GetMockItem(category: ItemCategory.Miscellaneous)
            };
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockItems);

            // Act
            var result = await _summaryService.GetItemCountAsync();

            // Assert
            Assert.Equal(6, result.TotalItems);
            Assert.Equal(2, result.Electronic);
            Assert.Equal(1, result.Keys);
            Assert.Equal(1, result.MediaEquipment);
            Assert.Equal(1, result.Tools);
            Assert.Equal(1, result.Miscellaneous);
        }

        #endregion

        #region GetLentItemsCountAsync Tests

        [Fact]
        public async Task GetLentItemsCountAsync_WithValidData_ShouldReturnCorrectCounts()
        {
            // Arrange
            var mockLentItems = SummaryMockData.GetMockLentItemsList();
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockLentItems);

            // Act
            var result = await _summaryService.GetLentItemsCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.TotalLentItems);
            Assert.Equal(3, result.CurrentlyLentItems); // ReturnedAt is null
            Assert.Equal(2, result.ReturnedLentItems); // ReturnedAt is not null
        }

        [Fact]
        public async Task GetLentItemsCountAsync_WithEmptyDatabase_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());

            // Act
            var result = await _summaryService.GetLentItemsCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalLentItems);
            Assert.Equal(0, result.CurrentlyLentItems);
            Assert.Equal(0, result.ReturnedLentItems);
        }

        [Fact]
        public async Task GetLentItemsCountAsync_WithOnlyCurrentlyLent_ShouldCountCorrectly()
        {
            // Arrange
            var mockLentItems = new List<LentItems>
            {
                new LentItems { Id = Guid.NewGuid(), ReturnedAt = null },
                new LentItems { Id = Guid.NewGuid(), ReturnedAt = null },
                new LentItems { Id = Guid.NewGuid(), ReturnedAt = null }
            };
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockLentItems);

            // Act
            var result = await _summaryService.GetLentItemsCountAsync();

            // Assert
            Assert.Equal(3, result.TotalLentItems);
            Assert.Equal(3, result.CurrentlyLentItems);
            Assert.Equal(0, result.ReturnedLentItems);
        }

        [Fact]
        public async Task GetLentItemsCountAsync_WithOnlyReturned_ShouldCountCorrectly()
        {
            // Arrange
            var mockLentItems = new List<LentItems>
            {
                new LentItems { Id = Guid.NewGuid(), ReturnedAt = DateTime.UtcNow.AddDays(-1) },
                new LentItems { Id = Guid.NewGuid(), ReturnedAt = DateTime.UtcNow.AddDays(-2) }
            };
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockLentItems);

            // Act
            var result = await _summaryService.GetLentItemsCountAsync();

            // Assert
            Assert.Equal(2, result.TotalLentItems);
            Assert.Equal(0, result.CurrentlyLentItems);
            Assert.Equal(2, result.ReturnedLentItems);
        }

        #endregion

        #region GetActiveUserCountAsync Tests

        [Fact]
        public async Task GetActiveUserCountAsync_WithValidData_ShouldReturnCorrectCounts()
        {
            // Arrange
            var mockUsers = SummaryMockData.GetMockActiveUsersList();
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(15, result.TotalActiveUsers); // Only "Online" users
            Assert.Equal(3, result.TotalActiveAdmins); // 1 SuperAdmin + 2 Admin
            Assert.Equal(3, result.TotalActiveStaffs); // 3 Staff
            Assert.Equal(4, result.TotalActiveTeachers); // 4 Teachers
            Assert.Equal(5, result.TotalActiveStudents); // 5 Students
        }

        [Fact]
        public async Task GetActiveUserCountAsync_WithEmptyDatabase_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalActiveUsers);
            Assert.Equal(0, result.TotalActiveAdmins);
            Assert.Equal(0, result.TotalActiveStaffs);
            Assert.Equal(0, result.TotalActiveTeachers);
            Assert.Equal(0, result.TotalActiveStudents);
        }

        [Fact]
        public async Task GetActiveUserCountAsync_WithOnlyOfflineUsers_ShouldReturnZeroCounts()
        {
            // Arrange
            var mockUsers = new List<User>
            {
                new User { Id = Guid.NewGuid(), Status = "Offline", UserRole = UserRole.Admin },
                new User { Id = Guid.NewGuid(), Status = "Offline", UserRole = UserRole.Student },
                new User { Id = Guid.NewGuid(), Status = "Offline", UserRole = UserRole.Teacher }
            };
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.Equal(0, result.TotalActiveUsers);
            Assert.Equal(0, result.TotalActiveAdmins);
            Assert.Equal(0, result.TotalActiveStaffs);
            Assert.Equal(0, result.TotalActiveTeachers);
            Assert.Equal(0, result.TotalActiveStudents);
        }

        [Fact]
        public async Task GetActiveUserCountAsync_WithOnlyStudents_ShouldCountCorrectly()
        {
            // Arrange
            var mockUsers = new List<User>
            {
                new Student { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Student },
                new Student { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Student },
                new Student { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Student }
            };
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.Equal(3, result.TotalActiveUsers);
            Assert.Equal(0, result.TotalActiveAdmins);
            Assert.Equal(0, result.TotalActiveStaffs);
            Assert.Equal(0, result.TotalActiveTeachers);
            Assert.Equal(3, result.TotalActiveStudents);
        }

        [Fact]
        public async Task GetActiveUserCountAsync_WithSuperAdminAndAdmin_ShouldCombineAdminCounts()
        {
            // Arrange
            var mockUsers = new List<User>
            {
                new User { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.SuperAdmin },
                new User { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Admin },
                new User { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Admin }
            };
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.Equal(3, result.TotalActiveUsers);
            Assert.Equal(3, result.TotalActiveAdmins); // SuperAdmin + Admin combined
        }

        [Fact]
        public async Task GetActiveUserCountAsync_WithMixedStatuses_ShouldOnlyCountOnlineUsers()
        {
            // Arrange
            var mockUsers = new List<User>
            {
                new User { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Student },
                new User { Id = Guid.NewGuid(), Status = "Offline", UserRole = UserRole.Student },
                new User { Id = Guid.NewGuid(), Status = "Online", UserRole = UserRole.Teacher },
                new User { Id = Guid.NewGuid(), Status = "Away", UserRole = UserRole.Staff }
            };
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(mockUsers);

            // Act
            var result = await _summaryService.GetActiveUserCountAsync();

            // Assert
            Assert.Equal(2, result.TotalActiveUsers); // Only "Online" status
            Assert.Equal(1, result.TotalActiveStudents);
            Assert.Equal(1, result.TotalActiveTeachers);
            Assert.Equal(0, result.TotalActiveStaffs);
        }

        #endregion

        #region Repository Verification Tests

        [Fact]
        public async Task GetOverallSummaryAsync_ShouldCallAllRepositories()
        {
            // Arrange
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Item>());
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            await _summaryService.GetOverallSummaryAsync();

            // Assert
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockLentItemsRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetItemCountAsync_ShouldOnlyCallItemRepository()
        {
            // Arrange
            _mockItemRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<Item>());

            // Act
            await _summaryService.GetItemCountAsync();

            // Assert
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockLentItemsRepository.Verify(x => x.GetAllAsync(), Times.Never);
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetLentItemsCountAsync_ShouldOnlyCallLentItemsRepository()
        {
            // Arrange
            _mockLentItemsRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<LentItems>());

            // Act
            await _summaryService.GetLentItemsCountAsync();

            // Assert
            _mockLentItemsRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Never);
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Never);
        }

        [Fact]
        public async Task GetActiveUserCountAsync_ShouldOnlyCallUserRepository()
        {
            // Arrange
            _mockUserRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<User>());

            // Act
            await _summaryService.GetActiveUserCountAsync();

            // Assert
            _mockUserRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockItemRepository.Verify(x => x.GetAllAsync(), Times.Never);
            _mockLentItemsRepository.Verify(x => x.GetAllAsync(), Times.Never);
        }

        #endregion
    }
}
