using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class Feedback
    {
        [Key]
        public int FeedbackID { get; set; }

        public int ReservationID { get; set; }

        [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5.")]
        public int Rating { get; set; }

        [StringLength(
            1000,
            ErrorMessage = "Feedback cannot exceed 1000 characters.")]
        public string? Comment { get; set; }

        public DateTime SubmittedAt { get; set; }

        public Reservation? Reservation { get; set; }
    }
}