using BackendTechnicalAssetsManagement.src.Classes;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.MockData
{
    public static class SummaryMockData
    {
        public static List<User> GetMockActiveUsersList()
        {
            return new List<User>
            {
                // SuperAdmin
                new User { Id = Guid.NewGuid(), Username = "superadmin", Status = "Online", UserRole = UserRole.SuperAdmin },
                
                // Admins
                new User { Id = Guid.NewGuid(), Username = "admin1", Status = "Online", UserRole = UserRole.Admin },
                new User { Id = Guid.NewGuid(), Username = "admin2", Status = "Online", UserRole = UserRole.Admin },
                
                // Staff
                new Staff { Id = Guid.NewGuid(), Username = "staff1", Status = "Online", UserRole = UserRole.Staff, Position = "IT Support" },
                new Staff { Id = Guid.NewGuid(), Username = "staff2", Status = "Online", UserRole = UserRole.Staff, Position = "HR" },
                new Staff { Id = Guid.NewGuid(), Username = "staff3", Status = "Online", UserRole = UserRole.Staff, Position = "Admin" },
                
                // Teachers
                new Teacher { Id = Guid.NewGuid(), Username = "teacher1", Status = "Online", UserRole = UserRole.Teacher, Department = "CS" },
                new Teacher { Id = Guid.NewGuid(), Username = "teacher2", Status = "Online", UserRole = UserRole.Teacher, Department = "IT" },
                new Teacher { Id = Guid.NewGuid(), Username = "teacher3", Status = "Online", UserRole = UserRole.Teacher, Department = "Math" },
                new Teacher { Id = Guid.NewGuid(), Username = "teacher4", Status = "Online", UserRole = UserRole.Teacher, Department = "Science" },
                
                // Students
                new Student { Id = Guid.NewGuid(), Username = "student1", Status = "Online", UserRole = UserRole.Student, Course = "CS" },
                new Student { Id = Guid.NewGuid(), Username = "student2", Status = "Online", UserRole = UserRole.Student, Course = "IT" },
                new Student { Id = Guid.NewGuid(), Username = "student3", Status = "Online", UserRole = UserRole.Student, Course = "CS" },
                new Student { Id = Guid.NewGuid(), Username = "student4", Status = "Online", UserRole = UserRole.Student, Course = "IT" },
                new Student { Id = Guid.NewGuid(), Username = "student5", Status = "Online", UserRole = UserRole.Student, Course = "CS" },
                
                // Offline users (should not be counted)
                new User { Id = Guid.NewGuid(), Username = "offline1", Status = "Offline", UserRole = UserRole.Student },
                new User { Id = Guid.NewGuid(), Username = "offline2", Status = "Offline", UserRole = UserRole.Teacher }
            };
        }

        public static List<LentItems> GetMockLentItemsList()
        {
            return new List<LentItems>
            {
                // Currently lent (ReturnedAt is null)
                new LentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Laptop",
                    UserId = Guid.NewGuid(),
                    BorrowerFullName = "John Doe",
                    LentAt = DateTime.UtcNow.AddDays(-2),
                    ReturnedAt = null,
                    Status = "Borrowed"
                },
                new LentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Projector",
                    UserId = Guid.NewGuid(),
                    BorrowerFullName = "Jane Smith",
                    LentAt = DateTime.UtcNow.AddDays(-1),
                    ReturnedAt = null,
                    Status = "Borrowed"
                },
                new LentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Camera",
                    UserId = Guid.NewGuid(),
                    BorrowerFullName = "Bob Johnson",
                    LentAt = DateTime.UtcNow.AddHours(-5),
                    ReturnedAt = null,
                    Status = "Borrowed"
                },
                
                // Returned items (ReturnedAt is not null)
                new LentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Tablet",
                    UserId = Guid.NewGuid(),
                    BorrowerFullName = "Alice Brown",
                    LentAt = DateTime.UtcNow.AddDays(-10),
                    ReturnedAt = DateTime.UtcNow.AddDays(-8),
                    Status = "Returned"
                },
                new LentItems
                {
                    Id = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Keyboard",
                    UserId = Guid.NewGuid(),
                    BorrowerFullName = "Charlie Wilson",
                    LentAt = DateTime.UtcNow.AddDays(-5),
                    ReturnedAt = DateTime.UtcNow.AddDays(-3),
                    Status = "Returned"
                }
            };
        }

        public static List<User> GetEmptyUsersList()
        {
            return new List<User>();
        }

        public static List<LentItems> GetEmptyLentItemsList()
        {
            return new List<LentItems>();
        }
    }
}
