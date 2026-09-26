namespace RROMS.Models
{
    public class MenuItem
    {
        public int MenuItemID { get; set; }

        public int CategoryID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public bool? Availability { get; set; }

        public string? ImageURL { get; set; }

        public MenuCategory? Category { get; set; }
    }
}