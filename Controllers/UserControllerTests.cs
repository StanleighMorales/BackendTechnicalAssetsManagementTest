using BackendTechnicalAssetsManagement.src.Authorization;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using BackendTechnicalAssetsManagement.src.Utils;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;
using static BackendTechnicalAssetsManagement.src.DTOs.User.UserProfileDtos;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace BackendTechnicalAssetsManagementTest.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IAuthorizationService> _mockAuthorizationService;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockAuthorizationService = new Mock<IAuthorizationService>();

            _controller = new UserController(
                _mockUserService.Object,
                _mockUserRepository.Object,
                _mockMapper.Object,
                _mockAuthorizationService.Object
            );
        }

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_ShouldReturnOkWithUsers()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Username = "user1" },
                new UserDto { Id = Guid.NewGuid(), Username = "user2" }
            };

            _mockUserService
                .Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Users retrieved successfully.", response.Message);
            Assert.Equal(2, ((IEnumerable<UserDto>)response.Data).Count());
        }

        [Fact]
        public async Task GetAllUsers_WithEmptyList_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            _mockUserService
                .Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(new List<UserDto>());

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<IEnumerable<object>>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Empty((IEnumerable<UserDto>)response.Data);
        }

        #endregion

        #region GetUserProfileById Tests

        [Fact]
        public async Task GetUserProfileById_WithValidId_ShouldReturnOkWithProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new Student { Id = userId, Username = "testuser" };
            var userProfile = new GetStudentProfileDto 
            { 
                Id = userId, 
                Username = "testuser", 
                UserRole = UserRole.Student 
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Mock the AuthorizeAsync method properly - it's an extension method
            _mockAuthorizationService
                .Setup(x => x.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success());

            _mockUserService
                .Setup(x => x.GetUserProfileByIdAsync(userId))
                .ReturnsAsync(userProfile);

            // Act
            var result = await _controller.GetUserProfileById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("User profile retrieved successfully.", response.Message);
        }

        [Fact]
        public async Task GetUserProfileById_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetUserProfileById(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("User profile not found.", response.Message);
        }

        [Fact]
        public async Task GetUserProfileById_WithUnauthorizedAccess_ShouldReturnForbid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new Student { Id = userId, Username = "testuser" };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Mock the AuthorizeAsync method properly - it's an extension method
            _mockAuthorizationService
                .Setup(x => x.AuthorizeAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<object>(),
                    It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Failed());

            // Act
            var result = await _controller.GetUserProfileById(userId);

            // Assert
            Assert.IsType<ForbidResult>(result.Result);
        }

        #endregion

        #region UpdateStudentProfile Tests

        [Fact]
        public async Task UpdateStudentProfile_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var updateDto = new UpdateStudentProfileDto
            {
                FirstName = "John",
                LastName = "Doe"
            };

            _mockUserService
                .Setup(x => x.UpdateStudentProfileAsync(studentId, updateDto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStudentProfile(studentId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Student profile updated successfully.", response.Message);
        }

        [Fact]
        public async Task UpdateStudentProfile_WithNonExistentStudent_ShouldReturnNotFound()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var updateDto = new UpdateStudentProfileDto();

            _mockUserService
                .Setup(x => x.UpdateStudentProfileAsync(studentId, updateDto))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateStudentProfile(studentId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Student not found.", response.Message);
        }

        [Fact]
        public async Task UpdateStudentProfile_WithInvalidImage_ShouldReturnBadRequest()
        {
            // Arrange
            var studentId = Guid.NewGuid();
            var updateDto = new UpdateStudentProfileDto();

            _mockUserService
                .Setup(x => x.UpdateStudentProfileAsync(studentId, updateDto))
                .ThrowsAsync(new ArgumentException("Invalid image format"));

            // Act
            var result = await _controller.UpdateStudentProfile(studentId, updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Invalid image format", response.Message);
        }

        #endregion

        #region UpdateTeacherProfile Tests

        [Fact]
        public async Task UpdateTeacherProfile_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var updateDto = new UpdateTeacherProfileDto
            {
                FirstName = "Jane",
                LastName = "Smith"
            };

            var teacher = new Teacher
            {
                Id = teacherId,
                Username = "teacher1",
                Email = "teacher@test.com",
                FirstName = "Jane",
                LastName = "Smith",
                UserRole = UserRole.Teacher
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(teacherId))
                .ReturnsAsync(teacher);

            _mockUserRepository
                .Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Setup user claims for authorization
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, teacherId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.UpdateTeacherProfile(teacherId, updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Teacher profile updated successfully.", response.Message);
        }

        [Fact]
        public async Task UpdateTeacherProfile_WithNonExistentTeacher_ShouldReturnNotFound()
        {
            // Arrange
            var teacherId = Guid.NewGuid();
            var updateDto = new UpdateTeacherProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(teacherId))
                .ReturnsAsync((User)null);

            // Setup user claims for authorization
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, teacherId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.UpdateTeacherProfile(teacherId, updateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Teacher not found.", response.Message);
        }

        #endregion

        #region UpdateUserProfile (Staff/Admin) Tests

        [Fact]
        public async Task UpdateUserProfile_WithValidData_ShouldReturnNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var updateDto = new UpdateStaffProfileDto
            {
                FirstName = "Admin",
                LastName = "User"
            };

            _mockUserService
                .Setup(x => x.UpdateStaffOrAdminProfileAsync(userId, updateDto, currentUserId))
                .Returns(Task.CompletedTask);

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.UpdateUserProfile(userId, updateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        #endregion

        #region ArchiveUser Tests

        [Fact]
        public async Task ArchiveUser_WithValidId_ShouldReturnOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockUserService
                .Setup(x => x.DeleteUserAsync(userId, currentUserId))
                .ReturnsAsync((true, string.Empty));

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.ArchiveUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("User has been successfully archived.", response.Message);
        }

        [Fact]
        public async Task ArchiveUser_WithNonExistentUser_ShouldReturnNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockUserService
                .Setup(x => x.DeleteUserAsync(userId, currentUserId))
                .ReturnsAsync((false, "User not found"));

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            // Act
            var result = await _controller.ArchiveUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
        }

        #endregion

        #region ImportStudents Tests

        [Fact]
        public async Task ImportStudents_WithValidFile_ShouldReturnOk()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("students.xlsx");
            mockFile.Setup(f => f.Length).Returns(1024);

            var importResult = new ImportStudentsResponseDto
            {
                SuccessCount = 5,
                FailureCount = 0,
                RegisteredStudents = new List<StudentRegistrationResult>()
            };

            _mockUserService
                .Setup(x => x.ImportStudentsFromExcelAsync(mockFile.Object))
                .ReturnsAsync(importResult);

            // Act
            var result = await _controller.ImportStudents(mockFile.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportStudentsResponseDto>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Contains("Success: 5", response.Message);
        }

        [Fact]
        public async Task ImportStudents_WithNoFile_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.ImportStudents(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportStudentsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal("No file uploaded.", response.Message);
        }

        [Fact]
        public async Task ImportStudents_WithInvalidFileExtension_ShouldReturnBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("students.txt");
            mockFile.Setup(f => f.Length).Returns(1024);

            // Act
            var result = await _controller.ImportStudents(mockFile.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<ImportStudentsResponseDto>>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Contains("Excel files", response.Message);
        }

        #endregion

        #region GetStudentByIdNumber Tests

        [Fact]
        public async Task GetStudentByIdNumber_WithValidIdNumber_ShouldReturnOk()
        {
            // Arrange
            var studentIdNumber = "2024-001";
            var student = new { Id = Guid.NewGuid(), StudentIdNumber = studentIdNumber };

            _mockUserService
                .Setup(x => x.GetStudentByIdNumberAsync(studentIdNumber))
                .ReturnsAsync(student);

            // Act
            var result = await _controller.GetStudentByIdNumber(studentIdNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal("Student retrieved successfully.", response.Message);
        }

        [Fact]
        public async Task GetStudentByIdNumber_WithNonExistentIdNumber_ShouldReturnNotFound()
        {
            // Arrange
            var studentIdNumber = "9999-999";

            _mockUserService
                .Setup(x => x.GetStudentByIdNumberAsync(studentIdNumber))
                .ReturnsAsync((object)null);

            // Act
            var result = await _controller.GetStudentByIdNumber(studentIdNumber);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ApiResponse<object>>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal("Student not found.", response.Message);
        }

        #endregion
    }
}
