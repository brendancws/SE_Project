using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }

        public int OrderID { get; set; }

        public DateTime? PaymentDate { get; set; }

        public decimal Amount { get; set; }

        public string? Method { get; set; }

        public string? Status { get; set; }

        public string? TransactionNo { get; set; }

        public CustomerOrder? Order { get; set; }
    }
}