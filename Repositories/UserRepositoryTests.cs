using AutoMapper;
using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.Data;
using BackendTechnicalAssetsManagement.src.Repository;
using Microsoft.EntityFrameworkCore;
using Moq;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.Repositories
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IMapper> _mockMapper;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            BackendTechnicalAssetsManagement.src.Extensions.ModelBuilderExtensions.SkipSeedData = true;

            _context = new AppDbContext(options);
            _mockMapper = new Mock<IMapper>();
            _repository = new UserRepository(_context, _mockMapper.Object);
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
        public async Task GetAllAsync_WithData_ShouldReturnAllUsers()
        {
            // Arrange
            var student = new Student
            {
                Id = Guid.NewGuid(),
                Username = "student1",
                Email = "student@test.com",
                FirstName = "John",
                LastName = "Doe",
                UserRole = UserRole.Student
            };

            var teacher = new Teacher
            {
                Id = Guid.NewGuid(),
                Username = "teacher1",
                Email = "teacher@test.com",
                FirstName = "Jane",
                LastName = "Smith",
                UserRole = UserRole.Teacher
            };

            await _context.Users.AddRangeAsync(new User[] { student, teacher });
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
        public async Task GetByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new Student
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal("testuser", result.Username);
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

        #region GetByUsernameAsync Tests

        [Fact]
        public async Task GetByUsernameAsync_WithValidUsername_ShouldReturnUser()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john@test.com",
                FirstName = "John",
                LastName = "Doe",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync("john.doe");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("john.doe", result.Username);
        }

        [Fact]
        public async Task GetByUsernameAsync_CaseInsensitive_ShouldReturnUser()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "John.Doe",
                Email = "john@test.com",
                FirstName = "John",
                LastName = "Doe",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByUsernameAsync("JOHN.DOE");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John.Doe", result.Username);
        }

        [Fact]
        public async Task GetByUsernameAsync_WithInvalidUsername_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByUsernameAsync("nonexistent");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByEmailAsync Tests

        [Fact]
        public async Task GetByEmailAsync_WithValidEmail_ShouldReturnUser()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync("test@example.com");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
        }

        [Fact]
        public async Task GetByEmailAsync_WithInvalidEmail_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByEmailAsync("nonexistent@example.com");

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region AddAsync Tests

        [Fact]
        public async Task AddAsync_WithValidUser_ShouldAddToContext()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "newuser",
                Email = "new@test.com",
                FirstName = "New",
                LastName = "User",
                UserRole = UserRole.Student
            };

            // Act
            var result = await _repository.AddAsync(user);
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(user.Id, result.Id);
            
            var savedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(savedUser);
            Assert.Equal("newuser", savedUser.Username);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_WithValidUser_ShouldUpdateInContext()
        {
            // Arrange
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "oldname",
                Email = "old@test.com",
                FirstName = "Old",
                LastName = "Name",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Modify the user
            user.FirstName = "Updated";
            user.LastName = "Name";

            // Act
            await _repository.UpdateAsync(user);
            await _context.SaveChangesAsync();

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated", updatedUser.FirstName);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ShouldRemoveFromContext()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new Student
            {
                Id = userId,
                Username = "deleteuser",
                Email = "delete@test.com",
                FirstName = "Delete",
                LastName = "User",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(userId);
            await _context.SaveChangesAsync();

            // Assert
            var deletedUser = await _context.Users.FindAsync(userId);
            Assert.Null(deletedUser);
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
            var user = new Student
            {
                Id = Guid.NewGuid(),
                Username = "savetest",
                Email = "save@test.com",
                FirstName = "Save",
                LastName = "Test",
                UserRole = UserRole.Student
            };

            await _context.Users.AddAsync(user);

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
    }
}
