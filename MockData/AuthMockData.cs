using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs.User;
using BackendTechnicalAssetsManagement.src.Models.DTOs.Users;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.MockData
{
    public static class AuthMockData
    {
        public static RegisterUserDto GetValidStudentRegisterDto()
        {
            return new RegisterUserDto
            {
                Username = "student123",
                Email = "student@test.com",
                PhoneNumber = "1234567890",
                Password = "Password123!",
                Role = UserRole.Student
            };
        }

        public static RegisterUserDto GetValidTeacherRegisterDto()
        {
            return new RegisterUserDto
            {
                Username = "teacher123",
                Email = "teacher@test.com",
                PhoneNumber = "0987654321",
                Password = "TeacherPass123!",
                Role = UserRole.Teacher
            };
        }

        public static RegisterUserDto GetInvalidPasswordRegisterDto()
        {
            return new RegisterUserDto
            {
                Username = "testuser",
                Email = "test@test.com",
                Password = "weak",
                Role = UserRole.Student
            };
        }

        public static LoginUserDto GetValidLoginDto()
        {
            return new LoginUserDto
            {
                Identifier = "testuser",
                Password = "Password123!"
            };
        }

        public static LoginUserDto GetInvalidUsernameLoginDto()
        {
            return new LoginUserDto
            {
                Identifier = "nonexistent",
                Password = "Password123!"
            };
        }

        public static LoginUserDto GetInvalidPasswordLoginDto()
        {
            return new LoginUserDto
            {
                Identifier = "testuser",
                Password = "WrongPassword123!"
            };
        }

        public static User GetMockUser(Guid? id = null)
        {
            return new User
            {
                Id = id ?? Guid.NewGuid(),
                Username = "testuser",
                Email = "test@test.com",
                PasswordHash = "hashedPassword",
                Status = "Offline",
                UserRole = UserRole.Student
            };
        }

        public static Student GetMockStudent(Guid? id = null)
        {
            return new Student
            {
                Id = id ?? Guid.NewGuid(),
                Username = "student123",
                Email = "student@test.com",
                PasswordHash = "hashedPassword",
                Status = "Offline",
                UserRole = UserRole.Student
            };
        }

        public static UserDto GetMockUserDto(Guid? id = null, string? username = null, string? email = null)
        {
            return new UserDto
            {
                Id = id ?? Guid.NewGuid(),
                Username = username ?? "testuser",
                Email = email ?? "test@test.com"
            };
        }

        public static ChangePasswordDto GetValidChangePasswordDto()
        {
            return new ChangePasswordDto
            {
                NewPassword = "NewPassword123!"
            };
        }

        public static IEnumerable<object[]> GetInvalidPasswords()
        {
            yield return new object[] { "short" }; // Too short
            yield return new object[] { "nouppercase123!" }; // No uppercase
            yield return new object[] { "NOLOWERCASE123!" }; // No lowercase
            yield return new object[] { "NoNumbers!" }; // No numbers
            yield return new object[] { "NoSpecialChar123" }; // No special characters
        }
    }
}
