using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class MenuCategory
    {
        [Key]
        public int CategoryID { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}