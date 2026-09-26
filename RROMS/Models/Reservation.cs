namespace RROMS.Models
{
    public class Reservation
    {
        public int ReservationID { get; set; }

        public int CustomerID { get; set; }

        public int TableID { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan Time { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public Customer? Customer { get; set; }

        public RestaurantTable? Table { get; set; }
    }
}