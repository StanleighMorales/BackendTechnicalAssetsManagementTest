using BackendTechnicalAssetsManagement.src.Classes;
using static BackendTechnicalAssetsManagement.src.Classes.Enums;

namespace BackendTechnicalAssetsManagementTest.MockData
{
    public static class ItemMockData
    {
        public static Item GetMockItem(
            Guid? id = null,
            ItemCategory? category = null,
            ItemCondition? condition = null,
            ItemStatus? status = null,
            string? itemType = null)
        {
            return new Item
            {
                Id = id ?? Guid.NewGuid(),
                SerialNumber = $"SN-{Guid.NewGuid().ToString().Substring(0, 8)}",
                ItemName = "Test Item",
                ItemType = itemType ?? "Laptop",
                ItemMake = "Dell",
                ItemModel = "XPS 15",
                Description = "Test item description",
                Category = category ?? ItemCategory.Electronics,
                Condition = condition ?? ItemCondition.Good,
                Status = status ?? ItemStatus.Available,
                Barcode = $"BC-{Guid.NewGuid().ToString().Substring(0, 8)}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static List<Item> GetMockItemList()
        {
            return new List<Item>
            {
                // Electronics - Various conditions and statuses
                GetMockItem(category: ItemCategory.Electronics, condition: ItemCondition.New, status: ItemStatus.Available, itemType: "Laptop"),
                GetMockItem(category: ItemCategory.Electronics, condition: ItemCondition.Good, status: ItemStatus.Borrowed, itemType: "Laptop"),
                GetMockItem(category: ItemCategory.Electronics, condition: ItemCondition.Defective, status: ItemStatus.Unavailable, itemType: "Tablet"),
                GetMockItem(category: ItemCategory.Electronics, condition: ItemCondition.Refurbished, status: ItemStatus.Available, itemType: "Monitor"),
                GetMockItem(category: ItemCategory.Electronics, condition: ItemCondition.NeedRepair, status: ItemStatus.Unavailable, itemType: "Keyboard"),
                
                // Keys
                GetMockItem(category: ItemCategory.Keys, condition: ItemCondition.Good, status: ItemStatus.Available, itemType: "Room Key"),
                GetMockItem(category: ItemCategory.Keys, condition: ItemCondition.Good, status: ItemStatus.Borrowed, itemType: "Cabinet Key"),
                
                // Media Equipment
                GetMockItem(category: ItemCategory.MediaEquipment, condition: ItemCondition.New, status: ItemStatus.Available, itemType: "Projector"),
                GetMockItem(category: ItemCategory.MediaEquipment, condition: ItemCondition.Good, status: ItemStatus.Available, itemType: "Camera"),
                GetMockItem(category: ItemCategory.MediaEquipment, condition: ItemCondition.Good, status: ItemStatus.Borrowed, itemType: "Microphone"),
                
                // Tools
                GetMockItem(category: ItemCategory.Tools, condition: ItemCondition.Good, status: ItemStatus.Available, itemType: "Screwdriver Set"),
                GetMockItem(category: ItemCategory.Tools, condition: ItemCondition.Refurbished, status: ItemStatus.Available, itemType: "Drill"),
                
                // Miscellaneous
                GetMockItem(category: ItemCategory.Miscellaneous, condition: ItemCondition.Good, status: ItemStatus.Available, itemType: "Cable"),
                GetMockItem(category: ItemCategory.Miscellaneous, condition: ItemCondition.New, status: ItemStatus.Available, itemType: "Adapter"),
                GetMockItem(category: ItemCategory.Miscellaneous, condition: ItemCondition.Good, status: ItemStatus.Borrowed, itemType: "Extension Cord")
            };
        }

        public static List<Item> GetEmptyItemList()
        {
            return new List<Item>();
        }
    }
}
