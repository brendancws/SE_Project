using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class StaffMember
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(80, MinimumLength = 3, ErrorMessage = "Name must be 3–80 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3–50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address (e.g. name@example.com).")]
        [StringLength(120)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+?6?01)[0-9]{1}-?[0-9]{7,8}$",
            ErrorMessage = "Enter a valid Malaysian phone (e.g. 0123456789 or 012-3456789).")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Staff";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}