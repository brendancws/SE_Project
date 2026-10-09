using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class RestaurantTable
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string TableNumber { get; set; } = string.Empty;

        [Range(1, 50)]
        public int Capacity { get; set; } = 2;

        [StringLength(60)]
        public string? Location { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}