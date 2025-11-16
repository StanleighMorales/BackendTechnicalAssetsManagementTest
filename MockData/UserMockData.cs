using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;
using static BackendTechnicalAssetsManagement.src.DTOs.User.UserProfileDtos;

namespace BackendTechnicalAssetsManagementTest.MockData
{
    public static class UserMockData
    {
        public static Student GetMockStudent(Guid? id = null)
        {
            return new Student
            {
                Id = id ?? Guid.NewGuid(),
                Username = "student123",
                Email = "student@test.com",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                PasswordHash = "hashedPassword",
                Status = "Active",
                UserRole = UserRole.Student,
                StudentIdNumber = "STU001",
                Year = "3rd Year",
                Section = "A",
                Course = "Computer Science"
            };
        }

        public static Teacher GetMockTeacher(Guid? id = null)
        {
            return new Teacher
            {
                Id = id ?? Guid.NewGuid(),
                Username = "teacher123",
                Email = "teacher@test.com",
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "0987654321",
                PasswordHash = "hashedPassword",
                Status = "Active",
                UserRole = UserRole.Teacher,
                Department = "Computer Science"
            };
        }

        public static Staff GetMockStaff(Guid? id = null)
        {
            return new Staff
            {
                Id = id ?? Guid.NewGuid(),
                Username = "staff123",
                Email = "staff@test.com",
                FirstName = "Bob",
                LastName = "Johnson",
                PhoneNumber = "5555555555",
                PasswordHash = "hashedPassword",
                Status = "Active",
                UserRole = UserRole.Staff,
                Position = "IT Support"
            };
        }

        public static User GetMockAdmin(Guid? id = null)
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                Username = "admin123",
                Email = "admin@test.com",
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "1111111111",
                PasswordHash = "hashedPassword",
                Status = "Active",
                UserRole = UserRole.Admin
            };
        }

        public static User GetMockSuperAdmin(Guid? id = null)
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                Username = "superadmin",
                Email = "superadmin@test.com",
                FirstName = "Super",
                LastName = "Admin",
                PhoneNumber = "9999999999",
                PasswordHash = "hashedPassword",
                Status = "Active",
                UserRole = UserRole.SuperAdmin
            };
        }

        public static GetStudentProfileDto GetMockStudentProfileDto(Guid? id = null)
        {
            return new GetStudentProfileDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = "student123",
                Email = "student@test.com",
                FirstName = "John",
                LastName = "Doe",
                PhoneNumber = "1234567890",
                StudentIdNumber = "STU001",
                Year = "3rd Year",
                Section = "A",
                Course = "Computer Science"
            };
        }

        public static GetTeacherProfileDto GetMockTeacherProfileDto(Guid? id = null)
        {
            return new GetTeacherProfileDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = "teacher123",
                Email = "teacher@test.com",
                FirstName = "Jane",
                LastName = "Smith",
                PhoneNumber = "0987654321",
                Department = "Computer Science"
            };
        }

        public static GetStaffProfileDto GetMockStaffProfileDto(Guid? id = null)
        {
            return new GetStaffProfileDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = "staff123",
                Email = "staff@test.com",
                FirstName = "Bob",
                LastName = "Johnson",
                PhoneNumber = "5555555555",
                Position = "IT Support"
            };
        }

        public static UserDto GetMockUserDto(Guid? id = null)
        {
            return new UserDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            };
        }

        public static UpdateUserProfileDto GetValidUpdateUserProfileDto()
        {
            return new UpdateUserProfileDto
            {
                FirstName = "Updated",
                LastName = "Name",
                PhoneNumber = "9999999999"
            };
        }

        public static UpdateStudentProfileDto GetValidUpdateStudentProfileDto()
        {
            return new UpdateStudentProfileDto
            {
                FirstName = "Updated",
                LastName = "Student",
                PhoneNumber = "8888888888",
                Year = "4th Year",
                Section = "B"
            };
        }

        public static UpdateStaffProfileDto GetValidUpdateStaffProfileDto()
        {
            return new UpdateStaffProfileDto
            {
                FirstName = "Updated",
                LastName = "Staff",
                PhoneNumber = "7777777777",
                Position = "HR"
            };
        }

        public static List<UserDto> GetMockUserDtoList()
        {
            return new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Username = "user1", Email = "user1@test.com" },
                new UserDto { Id = Guid.NewGuid(), Username = "user2", Email = "user2@test.com" },
                new UserDto { Id = Guid.NewGuid(), Username = "user3", Email = "user3@test.com" }
            };
        }
    }
}
