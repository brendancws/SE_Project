using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the "CustomerOnly" folder convention in Program.cs.
    public class IndexModel : PageModel
    {
        private const int PageSize = 5;

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Reservation> MyReservations { get; private set; }
            = new();

        public HashSet<int> FeedbackEligibleReservationIds
        {
            get;
            private set;
        } = new();

        public HashSet<int> FeedbackSubmittedReservationIds
        {
            get;
            private set;
        } = new();

        [BindProperty(Name = "p", SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalReservations { get; private set; }

        public int ActiveReservations { get; private set; }

        public int ConfirmedReservations { get; private set; }

        public int CancelledReservations { get; private set; }

        public int TotalPages { get; private set; } = 1;

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalReservations == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            TotalReservations == 0
                ? 0
                : Math.Min(
                    PageNumber * PageSize,
                    TotalReservations);

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

            await LoadReservationsAsync(customerId.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostCancelAsync(
            int id,
            int p = 1)
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            // Only this customer's own reservation can be found.
            Reservation? reservation =
                await _context.Reservations
                    .FirstOrDefaultAsync(item =>
                        item.ReservationID == id &&
                        item.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            if (!CanCancel(reservation))
            {
                ErrorMessage =
                    "This reservation can no longer be cancelled.";

                return RedirectToPage(
                    new
                    {
                        p = Math.Max(1, p)
                    });
            }

            reservation.Status = "Cancelled";

            _context.Notifications.Add(
                new Notification
                {
                    UserID = customerId.Value,
                    UserType = "Customer",
                    Message =
                        $"Reservation RES-{reservation.ReservationID:D6} " +
                        "has been cancelled.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

            await _context.SaveChangesAsync();

            StatusMessage =
                "Your reservation has been cancelled.";

            /*
             * The cancelled record remains in the list, so the current
             * page remains valid. Page clamping on GET still protects
             * against manually supplied out-of-range page numbers.
             */
            return RedirectToPage(
                new
                {
                    p = Math.Max(1, p)
                });
        }

        public async Task<IActionResult> OnGetStatusesAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            // Return only reservations belonging to this customer.
            var statuses = await _context.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.CustomerID == customerId.Value)
                .Select(reservation => new
                {
                    reservationId =
                        reservation.ReservationID,

                    status =
                        reservation.Status ?? "Pending"
                })
                .ToListAsync();

            return new JsonResult(statuses);
        }

        public bool CanLeaveFeedback(
            Reservation reservation)
        {
            bool isCancelled = string.Equals(
                reservation.Status,
                "Cancelled",
                StringComparison.OrdinalIgnoreCase);

            return
                !isCancelled &&
                FeedbackEligibleReservationIds.Contains(
                    reservation.ReservationID) &&
                !FeedbackSubmittedReservationIds.Contains(
                    reservation.ReservationID);
        }

        public bool HasSubmittedFeedback(
            Reservation reservation)
        {
            return FeedbackSubmittedReservationIds.Contains(
                reservation.ReservationID);
        }

        // Pending or Confirmed, and still in the future.
        public bool CanCancel(
            Reservation reservation)
        {
            bool activeStatus =
                string.Equals(
                    reservation.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    reservation.Status,
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase);

            DateTime start =
                reservation.Date.Date +
                reservation.Time;

            return
                activeStatus &&
                start > DateTime.Now;
        }

        private async Task LoadReservationsAsync(
            int customerId)
        {
            IQueryable<Reservation> ownedReservations =
                _context.Reservations
                    .AsNoTracking()
                    .Where(reservation =>
                        reservation.CustomerID == customerId);

            TotalReservations =
                await ownedReservations.CountAsync();

            ConfirmedReservations =
                await ownedReservations.CountAsync(reservation =>
                    reservation.Status == "Confirmed");

            CancelledReservations =
                await ownedReservations.CountAsync(reservation =>
                    reservation.Status == "Cancelled");

            /*
             * Active reservations use the same business rule as the
             * Cancel action: Pending or Confirmed and still in the future.
             *
             * Date and Time are evaluated after loading the small set of
             * active-status candidates because combining DateTime and
             * TimeSpan is not translated consistently by every provider.
             */
            List<Reservation> activeCandidates =
                await ownedReservations
                    .Where(reservation =>
                        reservation.Status == "Pending" ||
                        reservation.Status == "Confirmed")
                    .Select(reservation =>
                        new Reservation
                        {
                            Date = reservation.Date,
                            Time = reservation.Time,
                            Status = reservation.Status
                        })
                    .ToListAsync();

            ActiveReservations =
                activeCandidates.Count(CanCancel);

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalReservations /
                    (double)PageSize));

            PageNumber = Math.Clamp(
                PageNumber,
                1,
                TotalPages);

            MyReservations =
                await ownedReservations
                    .Include(reservation =>
                        reservation.Table)
                    .OrderByDescending(reservation =>
                        reservation.Date)
                    .ThenByDescending(reservation =>
                        reservation.Time)
                    .ThenByDescending(reservation =>
                        reservation.ReservationID)
                    .Skip(
                        (PageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .ToListAsync();

            List<int> displayedReservationIds =
                MyReservations
                    .Select(reservation =>
                        reservation.ReservationID)
                    .ToList();

            if (displayedReservationIds.Count == 0)
            {
                FeedbackEligibleReservationIds =
                    new HashSet<int>();

                FeedbackSubmittedReservationIds =
                    new HashSet<int>();

                return;
            }

            FeedbackEligibleReservationIds = (
                await _context.CustomerOrders
                    .AsNoTracking()
                    .Where(order =>
                        order.CustomerID == customerId &&
                        order.ReservationID.HasValue &&
                        displayedReservationIds.Contains(
                            order.ReservationID.Value) &&
                        order.Status == "Completed")
                    .Select(order =>
                        order.ReservationID!.Value)
                    .Distinct()
                    .ToListAsync()
            ).ToHashSet();

            FeedbackSubmittedReservationIds = (
                await _context.Feedbacks
                    .AsNoTracking()
                    .Where(feedback =>
                        displayedReservationIds.Contains(
                            feedback.ReservationID))
                    .Select(feedback =>
                        feedback.ReservationID)
                    .ToListAsync()
            ).ToHashSet();
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return
                int.TryParse(idValue, out int id) &&
                id > 0
                    ? id
                    : null;
        }
    }
}