using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [StringLength(20)]
        public string Method { get; set; } = "Cash";   // Cash / Card / Online

        [StringLength(20)]
        public string Status { get; set; } = "Pending";  // Pending / Paid / Refunded

        public DateTime PaidAt { get; set; } = DateTime.Now;
    }
}