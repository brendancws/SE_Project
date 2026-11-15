using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        public int CustomerID { get; set; }

        public int TableID { get; set; }

        public DateTime Date { get; set; }

        public TimeSpan Time { get; set; }

        public string? Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        public Customer? Customer { get; set; }

        public RestaurantTable? Table { get; set; }

        public int NumberOfGuests { get; set; }

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => ReservationID;
            set => ReservationID = value;
        }

        [NotMapped]
        public DateTime ReservationDate
        {
            get => Date.Date.Add(Time);

            set
            {
                Date = value.Date;
                Time = value.TimeOfDay;
            }
        }

        [NotMapped]
        public int PartySize
        {
            get => NumberOfGuests;
            set => NumberOfGuests = value;
        }

        [NotMapped]
        public string CustomerName =>
            Customer?.Name ?? string.Empty;

        [NotMapped]
        public string CustomerPhone =>
            Customer?.Phone ?? string.Empty;
    }
}