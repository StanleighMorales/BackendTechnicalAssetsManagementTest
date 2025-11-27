using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class BarcodeGeneratorServiceTests : IDisposable
    {
        private readonly Mock<ILentItemsRepository> _mockLentItemsRepository;
        private readonly BarcodeGeneratorService _barcodeGeneratorService;
        private readonly AppDbContext _mockDbContext;

        public BarcodeGeneratorServiceTests()
        {
            _mockLentItemsRepository = new Mock<ILentItemsRepository>();
            
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mockDbContext = new AppDbContext(options);

            _mockLentItemsRepository
                .Setup(x => x.GetDbContext())
                .Returns(_mockDbContext);

            _barcodeGeneratorService = new BarcodeGeneratorService(_mockLentItemsRepository.Object);
            
            // Enable SkipImageGeneration for performance
            BarcodeGeneratorService.SkipImageGeneration = true;
        }

        public void Dispose()
        {
            _mockDbContext?.Dispose();
            // Reset the static flag to prevent affecting other tests or production code
            BarcodeGeneratorService.SkipImageGeneration = false;
        }

        #region GenerateItemBarcode Tests

        [Fact]
        public void GenerateItemBarcode_WithValidSerialNumber_ShouldReturnFormattedBarcode()
        {
            // Arrange
            var serialNumber = "SN-12345";

            // Act
            var result = _barcodeGeneratorService.GenerateItemBarcode(serialNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ITEM-SN-12345", result);
            Assert.StartsWith("ITEM-", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void GenerateItemBarcode_WithEmptyOrNullSerialNumber_ShouldReturnPrefixOnly(string? serialNumber)
        {
            // Act
            var result = _barcodeGeneratorService.GenerateItemBarcode(serialNumber!);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ITEM-", result);
        }

        #endregion

        #region GenerateLentItemBarcodeAsync Tests

        [Fact]
        public async Task GenerateLentItemBarcodeAsync_WithNoExistingBarcodes_ShouldReturnSequence001()
        {
            // Arrange
            var testDate = new DateTime(2025, 11, 27);

            // Act
            var result = await _barcodeGeneratorService.GenerateLentItemBarcodeAsync(testDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("LENT-20251127-001", result);
            Assert.StartsWith("LENT-", result);
        }

        [Fact]
        public async Task GenerateLentItemBarcodeAsync_WithExistingBarcodes_ShouldIncrementSequence()
        {
            // Arrange
            var testDate = new DateTime(2025, 11, 27);
            var existingLentItem = new LentItems
            {
                Id = Guid.NewGuid(),
                Barcode = "LENT-20251127-001",
                ItemId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Status = "Pending"
            };

            _mockDbContext.Set<LentItems>().Add(existingLentItem);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _barcodeGeneratorService.GenerateLentItemBarcodeAsync(testDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("LENT-20251127-002", result);
        }

        [Fact]
        public async Task GenerateLentItemBarcodeAsync_WithMultipleBarcodeSameDay_ShouldReturnNextSequence()
        {
            // Arrange
            var testDate = new DateTime(2025, 11, 27);
            var lentItems = new List<LentItems>
            {
                new LentItems { Id = Guid.NewGuid(), Barcode = "LENT-20251127-001", ItemId = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "Pending" },
                new LentItems { Id = Guid.NewGuid(), Barcode = "LENT-20251127-002", ItemId = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "Pending" },
                new LentItems { Id = Guid.NewGuid(), Barcode = "LENT-20251127-005", ItemId = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = "Pending" }
            };

            _mockDbContext.Set<LentItems>().AddRange(lentItems);
            await _mockDbContext.SaveChangesAsync();

            // Act
            var result = await _barcodeGeneratorService.GenerateLentItemBarcodeAsync(testDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("LENT-20251127-006", result); // Should use max sequence (005) + 1
        }

        [Fact]
        public async Task GenerateLentItemBarcodeAsync_WithNullDate_ShouldUseCurrentDate()
        {
            // Act
            var result = await _barcodeGeneratorService.GenerateLentItemBarcodeAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("LENT-", result);
            var expectedDatePrefix = $"LENT-{DateTime.UtcNow.Date:yyyyMMdd}-";
            Assert.StartsWith(expectedDatePrefix, result);
        }

        #endregion

        #region GenerateBarcodeImage Tests

        [Fact]
        public void GenerateBarcodeImage_WithValidText_ShouldReturnNull_WhenSkipImageGenerationEnabled()
        {
            // Arrange
            var barcodeText = "ITEM-SN-12345";
            BarcodeGeneratorService.SkipImageGeneration = true;

            // Act
            var result = _barcodeGeneratorService.GenerateBarcodeImage(barcodeText);

            // Assert
            Assert.Null(result); // Should be null when SkipImageGeneration is true
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void GenerateBarcodeImage_WithEmptyOrNullText_ShouldReturnNull(string? barcodeText)
        {
            // Act
            var result = _barcodeGeneratorService.GenerateBarcodeImage(barcodeText!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GenerateBarcodeImage_WithSkipImageGenerationFlag_ShouldReturnNull()
        {
            // Arrange
            var barcodeText = "LENT-20251127-001";
            BarcodeGeneratorService.SkipImageGeneration = true;

            // Act
            var result = _barcodeGeneratorService.GenerateBarcodeImage(barcodeText);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region Static Method Tests

        [Fact]
        public void GenerateItemBarcodeStatic_WithValidSerialNumber_ShouldReturnFormattedBarcode()
        {
            // Arrange
            var serialNumber = "SN-STATIC-001";

            // Act
            var result = BarcodeGeneratorService.GenerateItemBarcodeStatic(serialNumber);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ITEM-SN-STATIC-001", result);
            Assert.StartsWith("ITEM-", result);
        }

        [Fact]
        public void GenerateBarcodeImageStatic_WithValidText_ShouldReturnNull_WhenSkipImageGenerationEnabled()
        {
            // Arrange
            var barcodeText = "ITEM-STATIC-TEST";
            BarcodeGeneratorService.SkipImageGeneration = true;

            // Act
            var result = BarcodeGeneratorService.GenerateBarcodeImageStatic(barcodeText);

            // Assert
            Assert.Null(result); // Should be null when SkipImageGeneration is true
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void GenerateBarcodeImageStatic_WithEmptyOrNullText_ShouldReturnNull(string? barcodeText)
        {
            // Act
            var result = BarcodeGeneratorService.GenerateBarcodeImageStatic(barcodeText!);

            // Assert
            Assert.Null(result);
        }

        #endregion


    }
}
