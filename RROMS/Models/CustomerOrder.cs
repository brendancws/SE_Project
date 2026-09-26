namespace RROMS.Models
{
    public class CustomerOrder
    {
        public int OrderID { get; set; }

        public int CustomerID { get; set; }

        public int? StaffID { get; set; }

        public int? ReservationID { get; set; }

        public DateTime? OrderDate { get; set; }

        public string? Status { get; set; }

        public decimal? TotalAmount { get; set; }

        public Customer? Customer { get; set; }

        public Staff? Staff { get; set; }

        public Reservation? Reservation { get; set; }
    }
}