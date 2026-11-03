
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Reservations
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Reservation Reservation { get; set; } = null!;

        public List<RestaurantTable> Tables { get; set; } = new();

        [BindProperty]
        public DateTime ReservationDate { get; set; }

        [BindProperty]
        public TimeSpan ReservationTime { get; set; }

        [BindProperty]
        public int TableId { get; set; }

        [BindProperty]
        public string NewStatus { get; set; } = "Pending";

        // Load reservation information
        public async Task<IActionResult> OnGetAsync(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            Reservation = reservation;

            ReservationDate = reservation.Date;
            ReservationTime = reservation.Time;
            TableId = reservation.TableID;
            NewStatus = reservation.Status ?? "Pending";

            Tables = await _context.RestaurantTables
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            return Page();
        }

        // Save changes to reservation
        public async Task<IActionResult> OnPostAsync(int id)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            Reservation = reservation;

            Tables = await _context.RestaurantTables
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            // Validate reservation status
            var allowedStatuses = new[]
            {
                "Pending",
                "Confirmed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(NewStatus))
            {
                ModelState.AddModelError(
                    "",
                    "Invalid reservation status.");
            }

            // Check selected table exists
            if (!Tables.Any(t => t.TableID == TableId))
            {
                ModelState.AddModelError(
                    "",
                    "Please select a valid table.");
            }

            // Validate date and time
            if (ReservationDate == default ||
                ReservationTime < TimeSpan.Zero ||
                ReservationTime >= TimeSpan.FromDays(1))
            {
                ModelState.AddModelError(
                    "",
                    "Please enter a valid reservation date and time.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            DateTime selectedDate = ReservationDate.Date;

            // Check for duplicate table reservations
            if (NewStatus != "Cancelled")
            {

                TimeSpan slotLength = TimeSpan.FromHours(2);
                TimeSpan selectedEndTime = ReservationTime.Add(slotLength);

                var sameDayReservations = await _context.Reservations
                    .AsNoTracking()
                    .Where(r =>
                        r.ReservationID != id &&
                        r.TableID == TableId &&
                        r.Date >= selectedDate &&
                        r.Date < selectedDate.AddDays(1) &&
                        r.Status != "Cancelled")
                    .ToListAsync();

                bool conflict = sameDayReservations.Any(r =>
                    r.Time < selectedEndTime &&
                    ReservationTime < r.Time.Add(slotLength));


                if (conflict)
                {
                    ModelState.AddModelError(
                        "",
                        "This table is already reserved at the selected date and time.");

                    return Page();
                }
            }

            // Capture the existing status before applying Staff changes.
            string previousStatus =
                reservation.Status ?? "Pending";

            // Update reservation
            reservation.Date = selectedDate;
            reservation.Time = ReservationTime;
            reservation.TableID = TableId;
            reservation.Status = NewStatus;

            bool statusChanged =
                !string.Equals(
                    previousStatus,
                    reservation.Status,
                    StringComparison.OrdinalIgnoreCase);

            if (statusChanged)
            {
                string normalizedStatus =
                    reservation.Status ?? "Pending";

                string statusMessage =
                    normalizedStatus.ToLowerInvariant() switch
                    {
                        "confirmed" =>
                            $"Reservation RES-{reservation.ReservationID:D6} " +
                            "has been confirmed.",

                        "cancelled" =>
                            $"Reservation RES-{reservation.ReservationID:D6} " +
                            "has been cancelled.",

                        _ =>
                            $"Reservation RES-{reservation.ReservationID:D6} " +
                            $"is now {normalizedStatus}."
                    };

                _context.Notifications.Add(
                    new Notification
                    {
                        UserID = reservation.CustomerID,
                        UserType = "Customer",
                        Message = statusMessage,
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    });
            }

            // Reservation and notification are saved together.
            await _context.SaveChangesAsync();

            return RedirectToPage(
                "/Staff/Reservations/Index");
        }
    }
}
