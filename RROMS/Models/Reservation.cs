using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? CustomerPhone { get; set; }

        public DateTime ReservationDate { get; set; }

        public int PartySize { get; set; } = 2;

        public int? TableId { get; set; }
        public RestaurantTable? Table { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";   // Pending / Confirmed / Cancelled / Completed

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}