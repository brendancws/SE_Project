namespace RROMS.Models
{
    public class MenuCategory
    {
        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}