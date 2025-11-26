using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Users;
using BackendTechnicalAssetsManagement.src.IRepository;
using BackendTechnicalAssetsManagement.src.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Services
{
    public class ArchiveUserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IArchiveUserRepository> _mockArchiveUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<AppDbContext> _mockContext;
        private readonly Mock<IDbContextTransaction> _mockTransaction;
        private readonly ArchiveUserService _archiveUserService;

        public ArchiveUserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockArchiveUserRepository = new Mock<IArchiveUserRepository>();
            _mockMapper = new Mock<IMapper>();
            
            // Create mock DbContext with in-memory database
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _mockContext = new Mock<AppDbContext>(options);
            
            // Setup mock transaction
            _mockTransaction = new Mock<IDbContextTransaction>();
            var mockDatabase = new Mock<Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade>(_mockContext.Object);
            mockDatabase.Setup(x => x.BeginTransactionAsync(default))
                .ReturnsAsync(_mockTransaction.Object);
            
            _mockContext.Setup(x => x.Database).Returns(mockDatabase.Object);

            _archiveUserService = new ArchiveUserService(
                _mockUserRepository.Object,
                _mockArchiveUserRepository.Object,
                _mockMapper.Object,
                _mockContext.Object
            );
        }

        #region ArchiveUserAsync Tests

        [Fact]
        public async Task ArchiveUserAsync_WithValidUser_ShouldArchiveSuccessfully()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var userToArchive = new Staff
            {
                Id = targetUserId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Staff,
                Status = "Offline",
                Position = "IT Staff"
            };

            var archivedUser = new ArchiveStaff
            {
                Id = Guid.NewGuid(),
                OriginalUserId = targetUserId,
                Username = "testuser",
                Email = "test@example.com",
                Status = "Archived"
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync(userToArchive);

            _mockMapper
                .Setup(x => x.Map<ArchiveUser>(userToArchive))
                .Returns(archivedUser);

            _mockArchiveUserRepository
                .Setup(x => x.AddAsync(It.IsAny<ArchiveUser>()))
                .ReturnsAsync(archivedUser);

            _mockArchiveUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockUserRepository
                .Setup(x => x.DeleteAsync(targetUserId))
                .Returns(Task.CompletedTask);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockTransaction
                .Setup(x => x.CommitAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(targetUserId, currentUserId);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(targetUserId), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.AddAsync(It.IsAny<ArchiveUser>()), Times.Once);
            _mockUserRepository.Verify(x => x.DeleteAsync(targetUserId), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(default), Times.Once);
        }

        [Fact]
        public async Task ArchiveUserAsync_WithNonExistentUser_ShouldReturnError()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync((User?)null);

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(targetUserId, currentUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("User not found.", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(targetUserId), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.AddAsync(It.IsAny<ArchiveUser>()), Times.Never);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task ArchiveUserAsync_WithSuperAdmin_ShouldReturnError()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var superAdminUser = new Staff
            {
                Id = targetUserId,
                Username = "superadmin",
                Email = "admin@example.com",
                UserRole = UserRole.SuperAdmin,
                Status = "Offline"
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync(superAdminUser);

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(targetUserId, currentUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("SuperAdmin users cannot be archived.", result.ErrorMessage);
            _mockArchiveUserRepository.Verify(x => x.AddAsync(It.IsAny<ArchiveUser>()), Times.Never);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task ArchiveUserAsync_SelfArchiving_ShouldReturnError()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(userId, userId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cannot archive your own account.", result.ErrorMessage);
            _mockUserRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockArchiveUserRepository.Verify(x => x.AddAsync(It.IsAny<ArchiveUser>()), Times.Never);
        }

        [Fact]
        public async Task ArchiveUserAsync_WithOnlineUser_ShouldReturnError()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var onlineUser = new Staff
            {
                Id = targetUserId,
                Username = "onlineuser",
                Email = "online@example.com",
                UserRole = UserRole.Staff,
                Status = "Online"
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync(onlineUser);

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(targetUserId, currentUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Cannot archive a user who is currently online.", result.ErrorMessage);
            _mockArchiveUserRepository.Verify(x => x.AddAsync(It.IsAny<ArchiveUser>()), Times.Never);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task ArchiveUserAsync_WithException_ShouldRollbackAndReturnError()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var currentUserId = Guid.NewGuid();
            var userToArchive = new Staff
            {
                Id = targetUserId,
                Username = "testuser",
                UserRole = UserRole.Staff,
                Status = "Offline"
            };

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(targetUserId))
                .ReturnsAsync(userToArchive);

            _mockMapper
                .Setup(x => x.Map<ArchiveUser>(userToArchive))
                .Throws(new Exception("Mapping failed"));

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.ArchiveUserAsync(targetUserId, currentUserId);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Archive operation failed", result.ErrorMessage);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(default), Times.Never);
        }

        #endregion

        #region RestoreUserAsync Tests

        [Fact]
        public async Task RestoreUserAsync_WithValidArchiveId_ShouldRestoreSuccessfully()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                OriginalUserId = Guid.NewGuid(),
                Username = "restoreduser",
                Email = "restored@example.com",
                Status = "Archived",
                Position = "IT Staff"
            };

            var restoredUser = new Staff
            {
                Id = archivedUser.OriginalUserId ?? Guid.NewGuid(),
                Username = "restoreduser",
                Email = "restored@example.com",
                Status = "Offline",
                Position = "IT Staff"
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockMapper
                .Setup(x => x.Map<User>(archivedUser))
                .Returns(restoredUser);

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(restoredUser);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockArchiveUserRepository
                .Setup(x => x.DeleteAsync(archiveUserId))
                .Returns(Task.CompletedTask);

            _mockArchiveUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockTransaction
                .Setup(x => x.CommitAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.RestoreUserAsync(archiveUserId);

            // Assert
            Assert.True(result);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Status == "Offline")), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.DeleteAsync(archiveUserId), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(default), Times.Once);
        }

        [Fact]
        public async Task RestoreUserAsync_WithNonExistentArchive_ShouldReturnFalse()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync((ArchiveUser?)null);

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.RestoreUserAsync(archiveUserId);

            // Assert
            Assert.False(result);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
            _mockUserRepository.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
        }

        [Fact]
        public async Task RestoreUserAsync_WithException_ShouldRollbackAndReturnFalse()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                Username = "testuser",
                Status = "Archived"
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockMapper
                .Setup(x => x.Map<User>(archivedUser))
                .Throws(new Exception("Mapping failed"));

            _mockTransaction
                .Setup(x => x.RollbackAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.RestoreUserAsync(archiveUserId);

            // Assert
            Assert.False(result);
            _mockTransaction.Verify(x => x.RollbackAsync(default), Times.Once);
            _mockTransaction.Verify(x => x.CommitAsync(default), Times.Never);
        }

        [Fact]
        public async Task RestoreUserAsync_ShouldSetStatusToOffline()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                OriginalUserId = Guid.NewGuid(),
                Username = "testuser",
                Status = "Archived"
            };

            var restoredUser = new Staff
            {
                Id = archivedUser.OriginalUserId ?? Guid.NewGuid(),
                Username = "testuser",
                Status = "SomeOtherStatus" // Will be overridden
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockMapper
                .Setup(x => x.Map<User>(archivedUser))
                .Returns(restoredUser);

            _mockUserRepository
                .Setup(x => x.AddAsync(It.IsAny<User>()))
                .ReturnsAsync(restoredUser);

            _mockUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockArchiveUserRepository
                .Setup(x => x.DeleteAsync(archiveUserId))
                .Returns(Task.CompletedTask);

            _mockArchiveUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            _mockTransaction
                .Setup(x => x.CommitAsync(default))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _archiveUserService.RestoreUserAsync(archiveUserId);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(x => x.AddAsync(It.Is<User>(u => u.Status == "Offline")), Times.Once);
        }

        #endregion

        #region GetAllArchivedUsersAsync Tests

        [Fact]
        public async Task GetAllArchivedUsersAsync_ShouldReturnAllArchivedUsers()
        {
            // Arrange
            var archivedUserDtos = new List<ArchiveUserDto>
            {
                new ArchiveUserDto
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    Email = "user1@example.com",
                    FirstName = "User",
                    LastName = "One"
                },
                new ArchiveUserDto
                {
                    Id = Guid.NewGuid(),
                    Username = "user2",
                    Email = "user2@example.com",
                    FirstName = "User",
                    LastName = "Two"
                }
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetAllArchiveUserDtosAsync())
                .ReturnsAsync(archivedUserDtos);

            // Act
            var result = await _archiveUserService.GetAllArchivedUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockArchiveUserRepository.Verify(x => x.GetAllArchiveUserDtosAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllArchivedUsersAsync_WithEmptyArchive_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyList = new List<ArchiveUserDto>();

            _mockArchiveUserRepository
                .Setup(x => x.GetAllArchiveUserDtosAsync())
                .ReturnsAsync(emptyList);

            // Act
            var result = await _archiveUserService.GetAllArchivedUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockArchiveUserRepository.Verify(x => x.GetAllArchiveUserDtosAsync(), Times.Once);
        }

        #endregion

        #region GetArchivedUserByIdAsync Tests

        [Fact]
        public async Task GetArchivedUserByIdAsync_WithValidId_ShouldReturnArchivedUser()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Status = "Archived"
            };

            var archiveUserDto = new ArchiveUserDto
            {
                Id = archiveUserId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockMapper
                .Setup(x => x.Map<ArchiveUserDto?>(archivedUser))
                .Returns(archiveUserDto);

            // Act
            var result = await _archiveUserService.GetArchivedUserByIdAsync(archiveUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveUserId, result.Id);
            Assert.Equal("testuser", result.Username);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
        }

        [Fact]
        public async Task GetArchivedUserByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync((ArchiveUser?)null);

            _mockMapper
                .Setup(x => x.Map<ArchiveUserDto?>(It.IsAny<ArchiveUser>()))
                .Returns((ArchiveUserDto?)null);

            // Act
            var result = await _archiveUserService.GetArchivedUserByIdAsync(archiveUserId);

            // Assert
            Assert.Null(result);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
        }

        #endregion

        #region PermanentDeleteArchivedUserAsync Tests

        [Fact]
        public async Task PermanentDeleteArchivedUserAsync_WithValidId_ShouldDeleteAndReturnTrue()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                Username = "testuser"
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockArchiveUserRepository
                .Setup(x => x.DeleteAsync(archiveUserId))
                .Returns(Task.CompletedTask);

            _mockArchiveUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _archiveUserService.PermanentDeleteArchivedUserAsync(archiveUserId);

            // Assert
            Assert.True(result);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.DeleteAsync(archiveUserId), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task PermanentDeleteArchivedUserAsync_WithNonExistentId_ShouldReturnFalse()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync((ArchiveUser?)null);

            // Act
            var result = await _archiveUserService.PermanentDeleteArchivedUserAsync(archiveUserId);

            // Assert
            Assert.False(result);
            _mockArchiveUserRepository.Verify(x => x.GetByIdAsync(archiveUserId), Times.Once);
            _mockArchiveUserRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
            _mockArchiveUserRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task PermanentDeleteArchivedUserAsync_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var archiveUserId = Guid.NewGuid();
            var archivedUser = new ArchiveStaff
            {
                Id = archiveUserId,
                Username = "testuser"
            };

            _mockArchiveUserRepository
                .Setup(x => x.GetByIdAsync(archiveUserId))
                .ReturnsAsync(archivedUser);

            _mockArchiveUserRepository
                .Setup(x => x.DeleteAsync(archiveUserId))
                .Returns(Task.CompletedTask);

            _mockArchiveUserRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _archiveUserService.PermanentDeleteArchivedUserAsync(archiveUserId);

            // Assert
            Assert.False(result);
            _mockArchiveUserRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
