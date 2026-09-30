using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Admin
    {
        [Key]
        public int AdminID { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? Role { get; set; }
    }
}