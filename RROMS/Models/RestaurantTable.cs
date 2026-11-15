using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class RestaurantTable
    {
        [Key]
        public int TableID { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Status { get; set; }

        public string? Location { get; set; }

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => TableID;
            set => TableID = value;
        }

        [NotMapped]
        public bool IsAvailable
        {
            get => string.Equals(
                Status,
                "Available",
                StringComparison.OrdinalIgnoreCase);

            set => Status = value
                ? "Available"
                : "Unavailable";
        }
    }
}