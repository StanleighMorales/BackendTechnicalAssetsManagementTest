using BackendTechnicalAssetsManagement.src.Utils;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace BackendTechnicalAssetsManagementTest.Utils
{
    public class ImageConverterUtilsTests
    {
        #region ConvertIFormFileToByteArray Tests

        [Fact]
        public void ConvertIFormFileToByteArray_WithValidFile_ShouldReturnByteArray()
        {
            // Arrange
            var content = "Test file content";
            var fileName = "test.jpg";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.CopyTo(It.IsAny<Stream>()))
                .Callback<Stream>(s => stream.CopyTo(s));

            // Act
            var result = ImageConverterUtils.ConvertIFormFileToByteArray(mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(content), result);
        }

        [Fact]
        public void ConvertIFormFileToByteArray_WithNullFile_ShouldReturnNull()
        {
            // Act
            var result = ImageConverterUtils.ConvertIFormFileToByteArray(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ConvertIFormFileToByteArray_WithEmptyFile_ShouldReturnNull()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            // Act
            var result = ImageConverterUtils.ConvertIFormFileToByteArray(mockFile.Object);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ConvertIFormFileToByteArray_WithLargeFile_ShouldReturnByteArray()
        {
            // Arrange
            var largeContent = new byte[1024 * 1024]; // 1MB
            new Random().NextBytes(largeContent);
            var stream = new MemoryStream(largeContent);
            
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(stream.Length);
            mockFile.Setup(f => f.FileName).Returns("large.png");
            mockFile.Setup(f => f.CopyTo(It.IsAny<Stream>()))
                .Callback<Stream>(s => stream.CopyTo(s));

            // Act
            var result = ImageConverterUtils.ConvertIFormFileToByteArray(mockFile.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(largeContent.Length, result.Length);
        }

        #endregion

        #region ValidateImage Tests

        [Fact]
        public void ValidateImage_WithNullImage_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(null));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateImage_WithValidJpgImage_ShouldNotThrow()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns("test.jpg");

            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateImage_WithValidPngImage_ShouldNotThrow()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2MB
            mockFile.Setup(f => f.FileName).Returns("test.png");

            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateImage_WithValidWebpImage_ShouldNotThrow()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(500 * 1024); // 500KB
            mockFile.Setup(f => f.FileName).Returns("test.webp");

            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateImage_WithOversizedImage_ShouldThrowArgumentException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB (exceeds 5MB limit)
            mockFile.Setup(f => f.FileName).Returns("large.jpg");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Contains("cannot exceed", exception.Message);
            Assert.Contains("5MB", exception.Message);
        }

        [Fact]
        public void ValidateImage_WithInvalidExtension_ShouldThrowArgumentException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns("test.txt");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Contains("Invalid image file type", exception.Message);
        }

        [Fact]
        public void ValidateImage_WithNoExtension_ShouldThrowArgumentException()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns("testfile");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Contains("Invalid image file type", exception.Message);
        }

        [Theory]
        [InlineData(".jpg")]
        [InlineData(".jpeg")]
        [InlineData(".png")]
        [InlineData(".gif")]
        [InlineData(".webp")]
        public void ValidateImage_WithAllowedExtensions_ShouldNotThrow(string extension)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns($"test{extension}");

            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(".JPG")]
        [InlineData(".PNG")]
        [InlineData(".JPEG")]
        public void ValidateImage_WithUppercaseExtensions_ShouldNotThrow(string extension)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns($"test{extension}");

            // Act & Assert
            var exception = Record.Exception(() => ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Null(exception);
        }

        [Theory]
        [InlineData(".pdf")]
        [InlineData(".doc")]
        [InlineData(".exe")]
        [InlineData(".zip")]
        public void ValidateImage_WithDisallowedExtensions_ShouldThrowArgumentException(string extension)
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            mockFile.Setup(f => f.FileName).Returns($"test{extension}");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                ImageConverterUtils.ValidateImage(mockFile.Object));
            Assert.Contains("Invalid image file type", exception.Message);
        }

        #endregion
    }
}
