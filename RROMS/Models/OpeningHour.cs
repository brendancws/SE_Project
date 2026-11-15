using System.ComponentModel.DataAnnotations;

namespace RROMS.Models
{
    public class OpeningHour
    {
        public int Id { get; set; }

        [Required]
        public DayOfWeek Day { get; set; }

        public TimeSpan OpenTime { get; set; } = new TimeSpan(9, 0, 0);

        public TimeSpan CloseTime { get; set; } = new TimeSpan(22, 0, 0);

        public bool IsClosed { get; set; } = false;
    }
}