using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Password { get; set; } = string.Empty;

        public string? Address { get; set; }

        public DateTime? DateRegistered { get; set; }

        public string? Status { get; set; }

        // S7 - Customer requests and preferences
        public string? SpecialRequest { get; set; }

        public string? DiningPreference { get; set; }
    }
}