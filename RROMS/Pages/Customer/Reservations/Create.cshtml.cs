using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class CreateModel : PageModel
    {
        // Every booking is a fixed 2-hour slot (same rule as the Staff module)
        private static readonly TimeSpan SlotLength = TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public BookingInputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class BookingInputModel
        {
            [Required(ErrorMessage = "Please choose a date.")]
            [DataType(DataType.Date)]
            [Display(Name = "Date")]
            public DateTime Date { get; set; }

            [Required(ErrorMessage = "Please choose a time.")]
            [Display(Name = "Time")]
            public TimeSpan Time { get; set; }

            [Required(ErrorMessage = "Please enter the number of guests.")]
            [Range(1, 50, ErrorMessage = "Number of guests must be between 1 and 50.")]
            [Display(Name = "Number of Guests")]
            public int NumberOfGuests { get; set; } = 2;
        }

        public void OnGet()
        {
            Input.Date = DateTime.Today.AddDays(1);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            DateTime date = Input.Date.Date;

            // 1. Time must be a real time of day
            if (Input.Time < TimeSpan.Zero || Input.Time >= TimeSpan.FromDays(1))
            {
                ModelState.AddModelError("Input.Time", "Please enter a valid time.");
                return Page();
            }

            // 2. Must be in the future
            if (date + Input.Time <= DateTime.Now)
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

            if (Input.NumberOfGuests > maxCapacity)
            {
                ModelState.AddModelError("Input.NumberOfGuests",
                    $"We can seat up to {maxCapacity} guests online. Please contact the restaurant for larger groups.");
                return Page();
            }

            // 4. Find the smallest free table that fits the party
            DateTime nextDay = date.AddDays(1);
            TimeSpan start = Input.Time;
            TimeSpan end = start.Add(SlotLength);

            // Load that day's active bookings, then compare the times in C#
            var sameDayBookings = await _context.Reservations
                .AsNoTracking()
                .Where(r => r.Date >= date
                         && r.Date < nextDay
                         && r.Status != "Cancelled")
                .Select(r => new { r.TableID, r.Time })
                .ToListAsync();

            // Two 2-hour slots overlap when each starts before the other ends
            var bookedTableIds = sameDayBookings
                .Where(r => r.Time < end && start < r.Time.Add(SlotLength))
                .Select(r => r.TableID)
                .ToList();

            var table = await _context.RestaurantTables
                .AsNoTracking()
                .Where(t => t.Capacity >= Input.NumberOfGuests
                         && !bookedTableIds.Contains(t.TableID))
                .OrderBy(t => t.Capacity)
                .ThenBy(t => t.TableNumber)
                .FirstOrDefaultAsync();

            if (table == null)
            {
                ModelState.AddModelError("",
                    "No table is available at this time for your party size. Please try another time or date.");
                return Page();
            }

            // 5. Save the booking
            var reservation = new Reservation
            {
                CustomerID = customerId.Value,
                TableID = table.TableID,
                Date = date,
                Time = Input.Time,
                NumberOfGuests = Input.NumberOfGuests,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            StatusMessage = $"Reservation made for table {table.TableNumber} on {date:dd MMM yyyy} at {Input.Time:hh\\:mm}. It is pending confirmation.";
            return RedirectToPage("/Customer/Reservations/Index");
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}