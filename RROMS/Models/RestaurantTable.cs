namespace RROMS.Models
{
    public class RestaurantTable
    {
        public int TableID { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string? Status { get; set; }

        public string? Location { get; set; }
    }
}