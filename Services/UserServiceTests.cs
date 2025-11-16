using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.IService;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using BackendTechnicalAssetsManagement.src.Services;
using BackendTechnicalAssetsManagementTest.MockData;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;
using static BackendTechnicalAssetsManagement.src.DTOs.User.UserProfileDtos;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IArchiveUserService> _mockArchiveUserService;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockArchiveUserService = new Mock<IArchiveUserService>();

            // Initialize the service with all mocks
            _userService = new UserService(
                _mockUserRepository.Object,
                _mockMapper.Object,
                _mockArchiveUserService.Object
            );
        }

        #region GetUserProfileByIdAsync Tests

        [Fact]
        public async Task GetUserProfileByIdAsync_WithStudentUser_ShouldReturnStudentProfileDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var student = UserMockData.GetMockStudent(userId);
            var studentProfileDto = UserMockData.GetMockStudentProfileDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(student);

            _mockMapper
                .Setup(x => x.Map<GetStudentProfileDto>(student))
                .Returns(studentProfileDto);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<GetStudentProfileDto>(result);
            var studentResult = result as GetStudentProfileDto;
            Assert.Equal(userId, studentResult!.Id);
            Assert.Equal("student123", studentResult.Username);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
            _mockMapper.Verify(x => x.Map<GetStudentProfileDto>(student), Times.Once);
        }

        [Fact]
        public async Task GetUserProfileByIdAsync_WithTeacherUser_ShouldReturnTeacherProfileDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var teacher = UserMockData.GetMockTeacher(userId);
            var teacherProfileDto = UserMockData.GetMockTeacherProfileDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(teacher);

            _mockMapper
                .Setup(x => x.Map<GetTeacherProfileDto>(teacher))
                .Returns(teacherProfileDto);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<GetTeacherProfileDto>(result);
            var teacherResult = result as GetTeacherProfileDto;
            Assert.Equal(userId, teacherResult!.Id);
            Assert.Equal("teacher123", teacherResult.Username);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserProfileByIdAsync_WithStaffUser_ShouldReturnStaffProfileDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var staff = UserMockData.GetMockStaff(userId);
            var staffProfileDto = UserMockData.GetMockStaffProfileDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(staff);

            _mockMapper
                .Setup(x => x.Map<GetStaffProfileDto>(staff))
                .Returns(staffProfileDto);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<GetStaffProfileDto>(result);
            var staffResult = result as GetStaffProfileDto;
            Assert.Equal(userId, staffResult!.Id);
            Assert.Equal("staff123", staffResult.Username);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserProfileByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.GetUserProfileByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        #endregion

        #region GetAllUsersAsync Tests

        [Fact]
        public async Task GetAllUsersAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var userDtos = UserMockData.GetMockUserDtoList();

            _mockUserRepository
                .Setup(x => x.GetAllUserDtosAsync())
                .ReturnsAsync(userDtos);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            _mockUserRepository.Verify(x => x.GetAllUserDtosAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithNoUsers_ShouldReturnEmptyList()
        {
            // Arrange
            _mockUserRepository
                .Setup(x => x.GetAllUserDtosAsync())
                .ReturnsAsync(new List<UserDto>());

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockUserRepository.Verify(x => x.GetAllUserDtosAsync(), Times.Once);
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = UserMockData.GetMockStudent(userId);
            var userDto = UserMockData.GetMockUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(user);

            _mockMapper
                .Setup(x => x.Map<UserDto?>(user))
                .Returns(userDto);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            _mockMapper
                .Setup(x => x.Map<UserDto?>(It.IsAny<User>()))
                .Returns((UserDto?)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.Once);
        }

        #endregion

        #region UpdateUserProfileAsync Tests

        [Fact]
        public async Task UpdateUserProfileAsync_WithValidData_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = UserMockData.GetMockStudent(userId);
            var updateDto = UserMockData.GetValidUpdateUserProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingUser))
                .Callback<UpdateUserProfileDto, User>((src, dest) =>
                {
                    dest.FirstName = src.FirstName;
                    dest.LastName = src.LastName;
                    dest.PhoneNumber = src.PhoneNumber;
                });

            _mockUserRepository
                .Setup(x => x.UpdateAsync(existingUser))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, updateDto);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(x => x.UpdateAsync(existingUser), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateUserProfileAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = UserMockData.GetValidUpdateUserProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateUserProfileAsync(userId, updateDto);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region UpdateStudentProfileAsync Tests

        [Fact]
        public async Task UpdateStudentProfileAsync_WithValidStudent_ShouldUpdateAndReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingStudent = UserMockData.GetMockStudent(userId);
            var updateDto = UserMockData.GetValidUpdateStudentProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(existingStudent);

            _mockMapper
                .Setup(x => x.Map(updateDto, existingStudent))
                .Callback<UpdateStudentProfileDto, Student>((src, dest) =>
                {
                    dest.FirstName = src.FirstName!;
                    dest.LastName = src.LastName!;
                    dest.PhoneNumber = src.PhoneNumber;
                    dest.Year = src.Year;
                    dest.Section = src.Section;
                });

            _mockUserRepository
                .Setup(x => x.UpdateAsync(existingStudent))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _userService.UpdateStudentProfileAsync(userId, updateDto);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(x => x.UpdateAsync(existingStudent), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStudentProfileAsync_WithNonStudentUser_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var teacher = UserMockData.GetMockTeacher(userId);
            var updateDto = UserMockData.GetValidUpdateStudentProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(teacher);

            // Act
            var result = await _userService.UpdateStudentProfileAsync(userId, updateDto);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStudentProfileAsync_WithNonExistentUser_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateDto = UserMockData.GetValidUpdateStudentProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _userService.UpdateStudentProfileAsync(userId, updateDto);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        #endregion

        #region UpdateStaffOrAdminProfileAsync Tests

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_AdminUpdatingStaff_ShouldSucceed()
        {
            // Arrange
            var adminId = Guid.NewGuid();
            var staffId = Guid.NewGuid();
            var admin = UserMockData.GetMockAdmin(adminId);
            var staff = UserMockData.GetMockStaff(staffId);
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(adminId))
                .ReturnsAsync(admin);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(staffId))
                .ReturnsAsync(staff);

            _mockMapper
                .Setup(x => x.Map(updateDto, staff))
                .Callback<UpdateStaffProfileDto, Staff>((src, dest) =>
                {
                    dest.FirstName = src.FirstName!;
                    dest.LastName = src.LastName!;
                    dest.Position = src.Position;
                });

            _mockUserRepository
                .Setup(x => x.UpdateAsync(staff))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _userService.UpdateStaffOrAdminProfileAsync(staffId, updateDto, adminId);

            // Assert
            _mockUserRepository.Verify(x => x.UpdateAsync(staff), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_SuperAdminUpdatingAdmin_ShouldSucceed()
        {
            // Arrange
            var superAdminId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var superAdmin = UserMockData.GetMockSuperAdmin(superAdminId);
            var admin = UserMockData.GetMockAdmin(adminId);
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(superAdminId))
                .ReturnsAsync(superAdmin);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(adminId))
                .ReturnsAsync(admin);

            _mockMapper
                .Setup(x => x.Map(updateDto, admin))
                .Callback<UpdateStaffProfileDto, User>((src, dest) =>
                {
                    dest.FirstName = src.FirstName;
                    dest.LastName = src.LastName;
                });

            _mockUserRepository
                .Setup(x => x.UpdateAsync(admin))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _userService.UpdateStaffOrAdminProfileAsync(adminId, updateDto, superAdminId);

            // Assert
            _mockUserRepository.Verify(x => x.UpdateAsync(admin), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_UserUpdatingOwnProfile_ShouldSucceed()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var staff = UserMockData.GetMockStaff(userId);
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(staff);

            _mockMapper
                .Setup(x => x.Map(updateDto, staff))
                .Callback<UpdateStaffProfileDto, Staff>((src, dest) =>
                {
                    dest.FirstName = src.FirstName;
                    dest.LastName = src.LastName;
                });

            _mockUserRepository
                .Setup(x => x.UpdateAsync(staff))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            await _userService.UpdateStaffOrAdminProfileAsync(userId, updateDto, userId);

            // Assert
            _mockUserRepository.Verify(x => x.UpdateAsync(staff), Times.Once);
            _mockUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_StaffUpdatingAdmin_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var staffId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var staff = UserMockData.GetMockStaff(staffId);
            var admin = UserMockData.GetMockAdmin(adminId);
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(staffId))
                .ReturnsAsync(staff);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(adminId))
                .ReturnsAsync(admin);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _userService.UpdateStaffOrAdminProfileAsync(adminId, updateDto, staffId));
            Assert.Contains("permission", exception.Message);
        }

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_WithNonExistentCurrentUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(currentUserId))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _userService.UpdateStaffOrAdminProfileAsync(targetUserId, updateDto, currentUserId));
        }

        [Fact]
        public async Task UpdateStaffOrAdminProfileAsync_WithNonExistentTargetUser_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();
            var admin = UserMockData.GetMockAdmin(currentUserId);
            var updateDto = UserMockData.GetValidUpdateStaffProfileDto();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(currentUserId))
                .ReturnsAsync(admin);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync((User?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _userService.UpdateStaffOrAdminProfileAsync(targetUserId, updateDto, currentUserId));
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_WithValidUser_ShouldArchiveAndReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockArchiveUserService
                .Setup(x => x.ArchiveUserAsync(userId, currentUserId))
                .ReturnsAsync((true, string.Empty));

            // Act
            var result = await _userService.DeleteUserAsync(userId, currentUserId);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.ErrorMessage);
            _mockArchiveUserService.Verify(x => x.ArchiveUserAsync(userId, currentUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteUserAsync_WhenArchiveFails_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var errorMessage = "Archive failed";

            _mockArchiveUserService
                .Setup(x => x.ArchiveUserAsync(userId, currentUserId))
                .ReturnsAsync((false, errorMessage));

            // Act
            var result = await _userService.DeleteUserAsync(userId, currentUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            _mockArchiveUserService.Verify(x => x.ArchiveUserAsync(userId, currentUserId), Times.Once);
        }

        #endregion
    }
}
