using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the CustomerOnly folder convention in Program.cs
    public class FeedbackModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public FeedbackModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public string BookingReference => $"RES-{Id:D6}";
        public DateTime ReservationDate { get; private set; }
        public TimeSpan ReservationTime { get; private set; }
        public string TableNumber { get; private set; } = "-";

        public bool AlreadySubmitted { get; private set; }
        public DateTime? ExistingSubmittedAt { get; private set; }
        public int? ExistingRating { get; private set; }
        public string? ExistingComment { get; private set; }

        [BindProperty]
        [Required(ErrorMessage = "Please choose a rating.")]
        [Range(1, 5, ErrorMessage = "Please choose a rating from 1 to 5.")]
        public int? Rating { get; set; }

        [BindProperty]
        [StringLength(
            1000,
            ErrorMessage = "Feedback cannot exceed 1000 characters.")]
        public string? Comment { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == Id &&
                    r.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            ReservationDate = reservation.Date;
            ReservationTime = reservation.Time;
            TableNumber =
                reservation.Table?.TableNumber ?? "-";

            bool hasCompletedOrder =
                await _context.CustomerOrders
                    .AsNoTracking()
                    .AnyAsync(o =>
                        o.ReservationID == Id &&
                        o.CustomerID == customerId.Value &&
                        o.Status == "Completed");

            if (!hasCompletedOrder ||
                string.Equals(
                    reservation.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            var existingFeedback =
                await _context.Feedbacks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f =>
                        f.ReservationID == Id);

            if (existingFeedback != null)
            {
                AlreadySubmitted = true;
                ExistingSubmittedAt =
                    existingFeedback.SubmittedAt;
                ExistingRating =
                    existingFeedback.Rating;
                ExistingComment =
                    existingFeedback.Comment;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == Id &&
                    r.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            ReservationDate = reservation.Date;
            ReservationTime = reservation.Time;
            TableNumber =
                reservation.Table?.TableNumber ?? "-";

            bool hasCompletedOrder =
                await _context.CustomerOrders
                    .AsNoTracking()
                    .AnyAsync(o =>
                        o.ReservationID == Id &&
                        o.CustomerID == customerId.Value &&
                        o.Status == "Completed");

            if (!hasCompletedOrder ||
                string.Equals(
                    reservation.Status,
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase))
            {
                return NotFound();
            }

            bool alreadyExists =
                await _context.Feedbacks
                    .AsNoTracking()
                    .AnyAsync(f =>
                        f.ReservationID == Id);

            if (alreadyExists)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Feedback has already been submitted for this reservation.");

                AlreadySubmitted = true;

                var existingFeedback =
                    await _context.Feedbacks
                        .AsNoTracking()
                        .FirstAsync(f =>
                            f.ReservationID == Id);

                ExistingSubmittedAt =
                    existingFeedback.SubmittedAt;
                ExistingRating =
                    existingFeedback.Rating;
                ExistingComment =
                    existingFeedback.Comment;

                return Page();
            }

            if (!Rating.HasValue || Rating.Value < 1 || Rating.Value > 5)
            {
                ModelState.AddModelError(
                    nameof(Rating),
                    "Please choose a rating from 1 to 5.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? cleanedComment =
                string.IsNullOrWhiteSpace(Comment)
                    ? null
                    : Comment.Trim();

            var feedback =
                new RROMS.Models.Feedback
                {
                    ReservationID = Id,
                    Rating = Rating.Value,
                    Comment = cleanedComment,
                    SubmittedAt = DateTime.Now
                };

            _context.Feedbacks.Add(feedback);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Handles a double-submit/race against the unique index.
                ModelState.AddModelError(
                    string.Empty,
                    "Feedback has already been submitted for this reservation.");

                return Page();
            }

            TempData["FeedbackMessage"] =
    $"Thank you. Your {Rating.Value}-star feedback was submitted for {BookingReference}.";

            return RedirectToPage(
                "/Customer/Reservations/Index");
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.TryParse(idValue, out int id) &&
                   id > 0
                ? id
                : null;
        }
    }
}