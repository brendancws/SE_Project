using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Reservation> MyReservations { get; private set; } = new();

        public HashSet<int> FeedbackEligibleReservationIds { get; private set; }
    = new();

        public HashSet<int> FeedbackSubmittedReservationIds { get; private set; }
            = new();

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            MyReservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Table)
                .Where(r => r.CustomerID == customerId.Value)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Time)
                .ToListAsync();

            var reservationIds = MyReservations
    .Select(r => r.ReservationID)
    .ToList();

            FeedbackEligibleReservationIds = (
    await _context.CustomerOrders
        .AsNoTracking()
        .Where(o =>
            o.CustomerID == customerId.Value &&
            o.ReservationID.HasValue &&
            reservationIds.Contains(o.ReservationID.Value) &&
            o.Status == "Completed")
        .Select(o => o.ReservationID!.Value)
        .Distinct()
        .ToListAsync()
).ToHashSet();

            FeedbackSubmittedReservationIds = (
                await _context.Feedbacks
                    .AsNoTracking()
                    .Where(f =>
                        reservationIds.Contains(f.ReservationID))
                    .Select(f => f.ReservationID)
                    .ToListAsync()
            ).ToHashSet();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            // Only this customer's own booking can be found
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationID == id
                                          && r.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            if (!CanCancel(reservation))
            {
                ErrorMessage = "This reservation can no longer be cancelled.";
                return RedirectToPage();
            }

            // Only the booking changes. Other bookings and the table are untouched.
            reservation.Status = "Cancelled";
            await _context.SaveChangesAsync();

            StatusMessage = "Your reservation has been cancelled.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetStatusesAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            // Only return reservations belonging to the current customer.
            var statuses = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.CustomerID == customerId.Value)
                .Select(r => new
                {
                    reservationId = r.ReservationID,
                    status = r.Status ?? "Pending"
                })
                .ToListAsync();

            return new JsonResult(statuses);
        }

        public bool CanLeaveFeedback(Reservation reservation)
        {
            bool isCancelled = string.Equals(
                reservation.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);

            return !isCancelled &&
                   FeedbackEligibleReservationIds.Contains(
                       reservation.ReservationID) &&
                   !FeedbackSubmittedReservationIds.Contains(
                       reservation.ReservationID);
        }

        public bool HasSubmittedFeedback(Reservation reservation)
        {
            return FeedbackSubmittedReservationIds.Contains(
                reservation.ReservationID);
        }

        // Pending or Confirmed, and still in the future
        public bool CanCancel(Reservation reservation)
        {
            bool activeStatus =
                string.Equals(reservation.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reservation.Status, "Confirmed", StringComparison.OrdinalIgnoreCase);

            DateTime start = reservation.Date.Date + reservation.Time;

            return activeStatus && start > DateTime.Now;
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}