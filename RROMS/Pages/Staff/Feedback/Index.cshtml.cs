using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Staff.Feedback
{
    // Protected by the StaffOnly folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class FeedbackView
        {
            public int FeedbackID { get; set; }
            public int ReservationID { get; set; }
            public string BookingReference =>
                $"RES-{ReservationID:D6}";

            public string CustomerName { get; set; }
                = "Unknown customer";

            public DateTime ReservationDate { get; set; }
            public TimeSpan ReservationTime { get; set; }
            public string TableNumber { get; set; } = "-";

            public int Rating { get; set; }
            public string? Comment { get; set; }
            public DateTime SubmittedAt { get; set; }
        }

        public List<FeedbackView> FeedbackItems
        {
            get;
            private set;
        } = new();

        public double AverageRating =>
            FeedbackItems.Count == 0
                ? 0
                : FeedbackItems.Average(f => f.Rating);

        public async Task OnGetAsync()
        {
            FeedbackItems = await _context.Feedbacks
                .AsNoTracking()
                .Include(f => f.Reservation)
                    .ThenInclude(r => r!.Customer)
                .Include(f => f.Reservation)
                    .ThenInclude(r => r!.Table)
                .OrderByDescending(f => f.SubmittedAt)
                .Select(f => new FeedbackView
                {
                    FeedbackID = f.FeedbackID,
                    ReservationID = f.ReservationID,
                    CustomerName =
                        f.Reservation != null &&
                        f.Reservation.Customer != null
                            ? f.Reservation.Customer.Name
                            : "Unknown customer",
                    ReservationDate =
                        f.Reservation != null
                            ? f.Reservation.Date
                            : default,
                    ReservationTime =
                        f.Reservation != null
                            ? f.Reservation.Time
                            : default,
                    TableNumber =
                        f.Reservation != null &&
                        f.Reservation.Table != null
                            ? f.Reservation.Table.TableNumber
                            : "-",
                    Rating = f.Rating,
                    Comment = f.Comment,
                    SubmittedAt = f.SubmittedAt
                })
                .ToListAsync();
        }
    }
}