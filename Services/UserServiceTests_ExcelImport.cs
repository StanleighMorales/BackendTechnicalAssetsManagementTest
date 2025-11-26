using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Microsoft.AspNetCore.Http;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    /// <summary>
    /// Pure unit tests for Excel import functionality using mocked IExcelReaderService
    /// These tests run fast (<50ms each) because they don't create real Excel files
    /// </summary>
    public class UserServiceTests_ExcelImport
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IArchiveUserService> _mockArchiveUserService;
        private readonly Mock<IPasswordHashingService> _mockPasswordHashingService;
        private readonly Mock<IExcelReaderService> _mockExcelReaderService;
        private readonly UserService _userService;

        public UserServiceTests_ExcelImport()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockArchiveUserService = new Mock<IArchiveUserService>();
            _mockPasswordHashingService = new Mock<IPasswordHashingService>();
            _mockExcelReaderService = new Mock<IExcelReaderService>();

            _userService = new UserService(
                _mockUserRepository.Object,
                _mockMapper.Object,
                _mockArchiveUserService.Object,
                _mockPasswordHashingService.Object,
                _mockExcelReaderService.Object
            );
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_WithValidExcelFile_ShouldImportStudents()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", null, 2)
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(1, result.TotalProcessed);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Single(result.RegisteredStudents);
            Assert.Equal("john.doe", result.RegisteredStudents[0].Username);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<Student>()), Times.Once);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_WithMissingRequiredColumns_ShouldReturnError()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var errorMessage = "Excel file must contain 'LastName' and 'FirstName' columns.";

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((new List<(string, string, string?, int)>(), errorMessage));

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(0, result.TotalProcessed);
            Assert.Equal(0, result.FailureCount); // FailureCount is set to students.Count which is 0
            Assert.Single(result.Errors);
            Assert.Contains("must contain 'LastName' and 'FirstName' columns", result.Errors[0]);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<Student>()), Times.Never);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_WithDuplicateStudentNames_ShouldSkipDuplicates()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var existingStudent = UserMockData.GetMockStudent();
            existingStudent.FirstName = "John";
            existingStudent.LastName = "Doe";
            existingStudent.MiddleName = "Michael";

            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", "Michael", 2),  // Duplicate
                ("Jane", "Smith", null, 3)       // New
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User> { existingStudent });

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(2, result.TotalProcessed);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Single(result.Errors);
            Assert.Contains("already exists in the database", result.Errors[0]);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<Student>()), Times.Once);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_WithDuplicateUsernames_ShouldGenerateUniqueUsernames()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var existingStudent = UserMockData.GetMockStudent();
            existingStudent.Username = "john.doe";

            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", null, 2)
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            var callCount = 0;
            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((string username) =>
                {
                    callCount++;
                    return callCount == 1 ? existingStudent : null;
                });

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Single(result.RegisteredStudents);
            Assert.Equal("john.doe1", result.RegisteredStudents[0].Username);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_WithEmptyRows_ShouldSkipAndReportErrors()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("", "", null, 2),           // Empty row
                ("Jane", "Smith", null, 3)   // Valid row
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(2, result.TotalProcessed);
            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            Assert.Single(result.Errors);
            Assert.Contains("Missing required fields", result.Errors[0]);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_UsernameGenerationWithMiddleName_ShouldIncludeMiddleName()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", "Michael", 2)
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Single(result.RegisteredStudents);
            Assert.Equal("john.michael.doe", result.RegisteredStudents[0].Username);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_UsernameGenerationWithoutMiddleName_ShouldExcludeMiddleName()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", null, 2)
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Single(result.RegisteredStudents);
            Assert.Equal("john.doe", result.RegisteredStudents[0].Username);
        }

        [Fact]
        public async Task ImportStudentsFromExcelAsync_PasswordGeneration_ShouldGenerateRandomPassword()
        {
            // Arrange
            var mockFile = CreateMockFormFile("students.xlsx");
            var mockStudents = new List<(string FirstName, string LastName, string? MiddleName, int RowNumber)>
            {
                ("John", "Doe", null, 2)
            };

            _mockExcelReaderService
                .Setup(x => x.ReadStudentsFromExcelAsync(mockFile))
                .ReturnsAsync((mockStudents, (string?)null));

            _mockUserRepository
                .Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<User>());

            _mockUserRepository
                .Setup(x => x.GetByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            _mockPasswordHashingService
                .Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedPassword");

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<Student>()))
                .ReturnsAsync((Student s) => s);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.ImportStudentsFromExcelAsync(mockFile);

            // Assert
            Assert.Equal(1, result.SuccessCount);
            Assert.Single(result.RegisteredStudents);
            Assert.NotNull(result.RegisteredStudents[0].GeneratedPassword);
            Assert.Equal(12, result.RegisteredStudents[0].GeneratedPassword.Length);
            _mockPasswordHashingService.Verify(x => x.HashPassword(It.IsAny<string>()), Times.Once);
        }

        private static IFormFile CreateMockFormFile(string fileName)
        {
            var content = "fake excel content";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var file = new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            };
            return file;
        }
    }
}
