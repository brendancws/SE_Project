using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class EditModel : PageModel
    {
        // Every booking is a fixed 2-hour slot (same rule as Create and Staff)
        private static readonly TimeSpan SlotLength = TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EditInputModel Input { get; set; } = new();

        public string CurrentTableNumber { get; private set; } = "-";

        public class EditInputModel
        {
            [Required(ErrorMessage = "Please select a reservation date.")]
            [DataType(DataType.Date)]
            [Display(Name = "Date")]
            public DateTime? Date { get; set; }

            [Required(ErrorMessage = "Please select a reservation time.")]
            [Display(Name = "Time")]
            public TimeSpan? Time { get; set; }

            [Required(ErrorMessage = "Please enter the number of guests.")]
            [Range(
                1,
                50,
                ErrorMessage = "Number of guests must be between 1 and 50.")]
            [Display(Name = "Number of Guests")]
            public int? NumberOfGuests { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            var reservation = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationID == id
                                          && r.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            if (!CanModify(reservation.Status, reservation.Date, reservation.Time))
            {
                TempData["ErrorMessage"] = "This reservation can no longer be changed.";
                return RedirectToPage("/Customer/Reservations/Index");
            }

            CurrentTableNumber = reservation.Table?.TableNumber ?? "-";

            Input = new EditInputModel
            {
                Date = reservation.Date.Date,
                Time = reservation.Time,
                NumberOfGuests = reservation.NumberOfGuests
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            // Tracked query: we may update this entity. Only this customer's own booking is found.
            var reservation = await _context.Reservations
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationID == id
                                          && r.CustomerID == customerId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            if (!CanModify(reservation.Status, reservation.Date, reservation.Time))
            {
                TempData["ErrorMessage"] = "This reservation can no longer be changed.";
                return RedirectToPage("/Customer/Reservations/Index");
            }

            CurrentTableNumber = reservation.Table?.TableNumber ?? "-";

            if (!ModelState.IsValid)
            {
                return Page();
            }

            DateTime date = Input.Date!.Value.Date;
            TimeSpan time = Input.Time!.Value;
            int numberOfGuests = Input.NumberOfGuests!.Value;

            // 1. Time must be a real time of day
            if (time < TimeSpan.Zero || time >= TimeSpan.FromDays(1))
            {
                ModelState.AddModelError("Input.Time", "Please enter a valid time.");
                return Page();
            }

            // 2. Must be in the future
            if (date + time <= DateTime.Now)
            {
                ModelState.AddModelError("", "Please choose a date and time in the future.");
                return Page();
            }

            // 3. Party must fit the biggest table
            int maxCapacity = await _context.RestaurantTables
                .MaxAsync(t => (int?)t.Capacity) ?? 0;

            if (maxCapacity == 0)
            {
                ModelState.AddModelError("", "No tables are set up yet. Please contact the restaurant.");
                return Page();
            }

            if (numberOfGuests > maxCapacity)
            {
                ModelState.AddModelError("Input.NumberOfGuests",
                    $"We can seat up to {maxCapacity} guests online. Please contact the restaurant for larger groups.");
                return Page();
            }

            // 4. Find a table. Ignore THIS booking so it can't clash with itself.
            DateTime nextDay = date.AddDays(1);
            TimeSpan start = time;
            TimeSpan end = start.Add(SlotLength);

            var sameDayBookings = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.ReservationID != id
                         && r.Date >= date
                         && r.Date < nextDay
                         && r.Status != "Cancelled")
                .Select(r => new { r.TableID, r.Time })
                .ToListAsync();

            var bookedTableIds = sameDayBookings
                .Where(r => r.Time < end && start < r.Time.Add(SlotLength))
                .Select(r => r.TableID)
                .ToList();

            var freeTables = await _context.RestaurantTables
                .AsNoTracking()
                .Where(t => t.Capacity >= numberOfGuests
         && !bookedTableIds.Contains(t.TableID))
                .OrderBy(t => t.Capacity)
                .ThenBy(t => t.TableNumber)
                .ToListAsync();

            // Prefer the current table if it still fits and is free
            var table = freeTables.FirstOrDefault(t => t.TableID == reservation.TableID)
                        ?? freeTables.FirstOrDefault();

            if (table == null)
            {
                ModelState.AddModelError("",
                    "No table is available at this time for your party size. Please try another time or date.");
                return Page();
            }

            // 5. Save the changes
            reservation.Date = date;
            reservation.Time = time;
            reservation.NumberOfGuests = numberOfGuests;
            reservation.TableID = table.TableID;

            // New details need staff approval again
            reservation.Status = "Pending";

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] =
                $"Reservation updated: table {table.TableNumber} " +
$"on {date:dd MMM yyyy} at {time:hh\\:mm}. " +
"It is pending confirmation.";

            return RedirectToPage("/Customer/Reservations/Index");
        }

        // Pending or Confirmed, and still in the future
        private static bool CanModify(string? status, DateTime date, TimeSpan time)
        {
            bool activeStatus =
                string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Confirmed", StringComparison.OrdinalIgnoreCase);

            return activeStatus && date.Date + time > DateTime.Now;
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}