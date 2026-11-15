using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => PaymentID;
            set => PaymentID = value;
        }

        [NotMapped]
        public int OrderId
        {
            get => OrderID;
            set => OrderID = value;
        }

        [NotMapped]
        public DateTime? PaidAt
        {
            get => PaymentDate;
            set => PaymentDate = value;
        }
    }
}