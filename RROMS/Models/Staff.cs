using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Staff
    {
        [Key]
        public int StaffID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Password { get; set; } = string.Empty;

        public string? Role { get; set; }

        public string? Status { get; set; }
    }
}