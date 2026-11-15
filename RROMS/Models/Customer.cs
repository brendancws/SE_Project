using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public string? SpecialRequest { get; set; }

        public string? DiningPreference { get; set; }

        // Admin compatibility aliases.
        [NotMapped]
        public int Id
        {
            get => CustomerID;
            set => CustomerID = value;
        }

        [NotMapped]
        public int CustomerId
        {
            get => CustomerID;
            set => CustomerID = value;
        }

        [NotMapped]
        public string FullName
        {
            get => Name;
            set => Name = value;
        }

        [NotMapped]
        public string PasswordHash
        {
            get => Password;
            set => Password = value;
        }

        [NotMapped]
        public DateTime? CreatedAt
        {
            get => DateRegistered;
            set => DateRegistered = value;
        }

        [NotMapped]
        public bool IsActive
        {
            get => string.Equals(
                Status,
                "Active",
                StringComparison.OrdinalIgnoreCase);

            set => Status = value
                ? "Active"
                : "Inactive";
        }
    }
}