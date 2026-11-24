using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RROMS.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int PasswordResetTokenID { get; set; }

        public int CustomerID { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool Used { get; set; }

        [ForeignKey(nameof(CustomerID))]
        public Customer? Customer { get; set; }
    }
}