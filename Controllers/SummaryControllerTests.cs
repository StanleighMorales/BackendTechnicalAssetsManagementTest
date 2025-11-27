using BackendTechnicalAssetsManagement.src.Controllers;
using BackendTechnicalAssetsManagement.src.DTOs.Statistics;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Utils;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class SummaryControllerTests
    {
        private readonly Mock<ISummaryService> _mockSummaryService;
        private readonly SummaryController _controller;

        public SummaryControllerTests()
        {
            _mockSummaryService = new Mock<ISummaryService>();
            _controller = new SummaryController(_mockSummaryService.Object);
        }

        #region GetOverallSummary Tests

        [Fact]
        public async Task GetOverallSummary_WithValidData_ShouldReturnOkWithSummary()
        {
            // Arrange
            var summaryDto = new SummaryDto
            {
                TotalItems = 100,
                TotalLentItems = 25,
                TotalActiveUsers = 50,
                TotalItemsCategories = 5,
                ItemStocks = new List<ItemStockDto>
                {
                    new ItemStockDto
                    {
                        ItemType = "Laptop",
                        TotalCount = 50,
                        AvailableCount = 35,
                        BorrowedCount = 15
                    }
                }
            };

            _mockSummaryService
                .Setup(x => x.GetOverallSummaryAsync())
                .ReturnsAsync(summaryDto);

            // Act
            var result = await _controller.GetOverallSummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<SummaryDto>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(100, response.Data.TotalItems);
            Assert.Equal(25, response.Data.TotalLentItems);
            Assert.Equal(50, response.Data.TotalActiveUsers);
            Assert.Single(response.Data.ItemStocks);
            
            _mockSummaryService.Verify(x => x.GetOverallSummaryAsync(), Times.Once);
        }

        [Fact]
        public async Task GetOverallSummary_WithEmptyData_ShouldReturnOkWithZeroCounts()
        {
            // Arrange
            var summaryDto = new SummaryDto
            {
                TotalItems = 0,
                TotalLentItems = 0,
                TotalActiveUsers = 0,
                TotalItemsCategories = 0,
                ItemStocks = new List<ItemStockDto>()
            };

            _mockSummaryService
                .Setup(x => x.GetOverallSummaryAsync())
                .ReturnsAsync(summaryDto);

            // Act
            var result = await _controller.GetOverallSummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<SummaryDto>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(0, response.Data.TotalItems);
            Assert.Equal(0, response.Data.TotalLentItems);
            Assert.Equal(0, response.Data.TotalActiveUsers);
            Assert.Empty(response.Data.ItemStocks);
        }

        [Fact]
        public async Task GetOverallSummary_WithMultipleStockItems_ShouldReturnAllItems()
        {
            // Arrange
            var summaryDto = new SummaryDto
            {
                TotalItems = 150,
                TotalLentItems = 40,
                TotalActiveUsers = 75,
                TotalItemsCategories = 2,
                ItemStocks = new List<ItemStockDto>
                {
                    new ItemStockDto { ItemType = "Laptop", TotalCount = 50, AvailableCount = 30, BorrowedCount = 20 },
                    new ItemStockDto { ItemType = "Mouse", TotalCount = 100, AvailableCount = 80, BorrowedCount = 20 }
                }
            };

            _mockSummaryService
                .Setup(x => x.GetOverallSummaryAsync())
                .ReturnsAsync(summaryDto);

            // Act
            var result = await _controller.GetOverallSummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<SummaryDto>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.NotNull(response.Data);
            Assert.Equal(2, response.Data.ItemStocks.Count);
            Assert.Contains(response.Data.ItemStocks, s => s.ItemType == "Laptop");
            Assert.Contains(response.Data.ItemStocks, s => s.ItemType == "Mouse");
        }

        [Fact]
        public async Task GetOverallSummary_ShouldIncludeSuccessMessage()
        {
            // Arrange
            var summaryDto = new SummaryDto
            {
                TotalItems = 10,
                TotalLentItems = 5,
                TotalActiveUsers = 20,
                TotalItemsCategories = 3,
                ItemStocks = new List<ItemStockDto>()
            };

            _mockSummaryService
                .Setup(x => x.GetOverallSummaryAsync())
                .ReturnsAsync(summaryDto);

            // Act
            var result = await _controller.GetOverallSummary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<SummaryDto>>(okResult.Value);
            
            Assert.True(response.Success);
            Assert.Contains("retrieved successfully", response.Message);
        }

        #endregion
    }
}
