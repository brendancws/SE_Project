using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Name must be 2–80 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between 0.01 and 9999.99.")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Please select a category.")]
        public int MenuCategoryId { get; set; }

        public MenuCategory? Category { get; set; }

        public bool IsAvailable { get; set; } = true;

        [StringLength(300)]
        public string? ImageUrl { get; set; }
    }
}