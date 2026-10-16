
using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        // Stores a securely hashed password, never plaintext.
        [Required]
        public string Password { get; set; } = string.Empty;

        public string? Role { get; set; }

        public string? Status { get; set; }
    }
}
