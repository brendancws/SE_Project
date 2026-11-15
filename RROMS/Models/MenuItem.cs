using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class MenuItem
    {
        [Key]
        public int MenuItemID { get; set; }

        public int CategoryID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public bool? Availability { get; set; }

        public string? ImageURL { get; set; }

        public MenuCategory? Category { get; set; }

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => MenuItemID;
            set => MenuItemID = value;
        }

        [NotMapped]
        public int MenuCategoryId
        {
            get => CategoryID;
            set => CategoryID = value;
        }

        [NotMapped]
        public string? ImageUrl
        {
            get => ImageURL;
            set => ImageURL = value;
        }

        [NotMapped]
        public bool IsAvailable
        {
            get => Availability ?? false;
            set => Availability = value;
        }
    }
}