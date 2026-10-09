using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class AdminUser
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Role { get; set; } = "Admin";
    }
}