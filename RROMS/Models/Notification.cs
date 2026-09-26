namespace RROMS.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }

        public int UserID { get; set; }

        public string UserType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }

        public bool? IsRead { get; set; }
    }
}