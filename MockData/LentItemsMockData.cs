using BackendTechnicalAssetsManagement.src.Classes;
using BackendTechnicalAssetsManagement.src.DTOs;
using BackendTechnicalAssetsManagement.src.DTOs.Archive.LentItems;
using BackendTechnicalAssetsManagement.src.DTOs.LentItems;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.MockData
{
    public static class LentItemsMockData
    {
        public static LentItems GetMockLentItem(Guid? id = null, string status = "Borrowed")
        {
            return new LentItems
            {
                Id = id ?? Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Laptop",
                UserId = Guid.NewGuid(),
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                StudentIdNumber = "STU001",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10:00-11:00",
                LentAt = DateTime.UtcNow.AddDays(-1),
                Status = status,
                Barcode = "LENT-001",
                IsHiddenFromUser = false
            };
        }

        public static LentItems GetMockPendingLentItem(Guid? id = null, Guid? itemId = null)
        {
            return new LentItems
            {
                Id = id ?? Guid.NewGuid(),
                ItemId = itemId ?? Guid.NewGuid(),
                ItemName = "Test Projector",
                UserId = Guid.NewGuid(),
                BorrowerFullName = "Jane Smith",
                BorrowerRole = "Teacher",
                Room = "Room 202",
                SubjectTimeSchedule = "TTH 14:00-15:00",
                Status = "Pending",
                Barcode = "LENT-002",
                IsHiddenFromUser = false
            };
        }

        public static LentItems GetMockReturnedLentItem(Guid? id = null)
        {
            return new LentItems
            {
                Id = id ?? Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Test Tablet",
                UserId = Guid.NewGuid(),
                BorrowerFullName = "Bob Johnson",
                BorrowerRole = "Student",
                StudentIdNumber = "STU002",
                Room = "Room 303",
                SubjectTimeSchedule = "MWF 13:00-14:00",
                LentAt = DateTime.UtcNow.AddDays(-3),
                ReturnedAt = DateTime.UtcNow.AddDays(-1),
                Status = "Returned",
                Barcode = "LENT-003",
                IsHiddenFromUser = false
            };
        }

        public static CreateLentItemDto GetValidCreateLentItemDto(Guid? itemId = null, Guid? userId = null)
        {
            return new CreateLentItemDto
            {
                ItemId = itemId ?? Guid.NewGuid(),
                UserId = userId ?? Guid.NewGuid(),
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10:00-11:00",
                Status = "Borrowed",
                Remarks = "Test remarks"
            };
        }

        public static CreateLentItemsForGuestDto GetValidCreateLentItemsForGuestDto(Guid? itemId = null)
        {
            return new CreateLentItemsForGuestDto
            {
                ItemId = itemId ?? Guid.NewGuid(),
                BorrowerFirstName = "Guest",
                BorrowerLastName = "User",
                BorrowerRole = "Student",
                StudentIdNumber = "GUEST001",
                TeacherFirstName = "Teacher",
                TeacherLastName = "Name",
                Room = "Room 404",
                SubjectTimeSchedule = "TTH 09:00-10:00",
                Status = "Borrowed"
            };
        }

        public static UpdateLentItemDto GetValidUpdateLentItemDto()
        {
            return new UpdateLentItemDto
            {
                Room = "Room 505",
                SubjectTimeSchedule = "MWF 15:00-16:00",
                Status = "Returned",
                Remarks = "Updated remarks"
            };
        }

        public static ScanLentItemDto GetValidScanLentItemDto(LentItemsStatus status = LentItemsStatus.Borrowed)
        {
            return new ScanLentItemDto
            {
                LentItemsStatus = status
            };
        }

        public static LentItemsDto GetMockLentItemsDto(Guid? id = null)
        {
            return new LentItemsDto
            {
                Id = id ?? Guid.NewGuid(),
                BorrowerFullName = "John Doe",
                BorrowerRole = "Student",
                Room = "Room 101",
                SubjectTimeSchedule = "MWF 10:00-11:00",
                Status = "Borrowed",
                Barcode = "LENT-001"
            };
        }

        public static List<LentItems> GetMockLentItemsList()
        {
            return new List<LentItems>
            {
                GetMockLentItem(Guid.NewGuid(), "Borrowed"),
                GetMockPendingLentItem(Guid.NewGuid()),
                GetMockReturnedLentItem(Guid.NewGuid())
            };
        }

        public static List<LentItemsDto> GetMockLentItemsDtoList()
        {
            return new List<LentItemsDto>
            {
                GetMockLentItemsDto(Guid.NewGuid()),
                GetMockLentItemsDto(Guid.NewGuid()),
                GetMockLentItemsDto(Guid.NewGuid())
            };
        }
    }
}
