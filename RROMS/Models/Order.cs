using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class Order
    {
        public int Id { get; set; }

        [StringLength(80)]
        public string CustomerName { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Pending";   // Pending / Preparing / Ready / Served / Cancelled

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}