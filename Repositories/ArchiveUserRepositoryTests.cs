using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;
using AutoMapper;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.Users;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class ArchiveUserRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ArchiveUserRepository _repository;

        public ArchiveUserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _mockMapper = new Mock<IMapper>();
            _repository = new ArchiveUserRepository(_context, _mockMapper.Object);
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
        public async Task GetAllAsync_WithData_ShouldReturnAllArchives()
        {
            // Arrange
            var archive1 = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "archived_user1",
                Email = "user1@archived.com",
                FirstName = "Archived",
                LastName = "User1",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            var archive2 = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "archived_user2",
                Email = "user2@archived.com",
                FirstName = "Archived",
                LastName = "User2",
                UserRole = UserRole.Teacher,
                ArchivedAt = DateTime.UtcNow
            };

            await _context.ArchiveUsers.AddRangeAsync(new[] { archive1, archive2 });
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
        public async Task GetByIdAsync_WithValidId_ShouldReturnArchive()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveUser
            {
                Id = archiveId,
                Username = "archived_user3",
                Email = "user3@archived.com",
                FirstName = "Archived",
                LastName = "User3",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            await _context.ArchiveUsers.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(archiveId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archiveId, result.Id);
            Assert.Equal("archived_user3", result.Username);
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

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidArchive_ShouldAddToContext()
        {
            // Arrange
            var archive = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "new_archived_user",
                Email = "newuser@archived.com",
                FirstName = "New",
                LastName = "User",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            // Act
            var result = await _repository.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(archive.Id, result.Id);
            
            var savedArchive = await _context.ArchiveUsers.FindAsync(archive.Id);
            Assert.NotNull(savedArchive);
            Assert.Equal("new_archived_user", savedArchive.Username);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var archiveId = Guid.NewGuid();
            var archive = new ArchiveUser
            {
                Id = archiveId,
                Username = "to_delete",
                Email = "delete@archived.com",
                FirstName = "To",
                LastName = "Delete",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            await _context.ArchiveUsers.AddAsync(archive);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(archiveId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedArchive = await _context.ArchiveUsers.FindAsync(archiveId);
            Assert.Null(deletedArchive);
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
            var archive = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "save_test",
                Email = "save@archived.com",
                FirstName = "Save",
                LastName = "Test",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            await _context.ArchiveUsers.AddAsync(archive);

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

        #region GetAllArchiveUserDtosAsync Tests

        [Fact(Skip = "Requires full AutoMapper configuration with ConfigurationProvider")]
        public async Task GetAllArchiveUserDtosAsync_WithNoData_ShouldReturnEmptyList()
        {
            // Arrange
            _mockMapper.Setup(m => m.Map<IEnumerable<ArchiveUserDto>>(It.IsAny<IEnumerable<ArchiveUser>>()))
                .Returns(new List<ArchiveUserDto>());

            // Act
            var result = await _repository.GetAllArchiveUserDtosAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact(Skip = "Requires full AutoMapper configuration with ConfigurationProvider")]
        public async Task GetAllArchiveUserDtosAsync_WithData_ShouldReturnMappedDtos()
        {
            // Arrange
            var archive1 = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "dto_user1",
                Email = "dto1@archived.com",
                FirstName = "DTO",
                LastName = "User1",
                UserRole = UserRole.Student,
                ArchivedAt = DateTime.UtcNow
            };

            var archive2 = new ArchiveUser
            {
                Id = Guid.NewGuid(),
                Username = "dto_user2",
                Email = "dto2@archived.com",
                FirstName = "DTO",
                LastName = "User2",
                UserRole = UserRole.Teacher,
                ArchivedAt = DateTime.UtcNow
            };

            await _context.ArchiveUsers.AddRangeAsync(new[] { archive1, archive2 });
            await _context.SaveChangesAsync();

            var expectedDtos = new List<ArchiveUserDto>
            {
                new ArchiveUserDto { Id = archive1.Id, Username = "dto_user1" },
                new ArchiveUserDto { Id = archive2.Id, Username = "dto_user2" }
            };

            _mockMapper.Setup(m => m.Map<IEnumerable<ArchiveUserDto>>(It.IsAny<IEnumerable<ArchiveUser>>()))
                .Returns(expectedDtos);

            // Act
            var result = await _repository.GetAllArchiveUserDtosAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockMapper.Verify(m => m.Map<IEnumerable<ArchiveUserDto>>(It.IsAny<IEnumerable<ArchiveUser>>()), Times.Once);
        }

        #endregion
    }
}
