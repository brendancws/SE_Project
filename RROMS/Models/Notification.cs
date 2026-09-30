using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        public int UserID { get; set; }

        public string UserType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public bool? IsRead { get; set; }
    }
}