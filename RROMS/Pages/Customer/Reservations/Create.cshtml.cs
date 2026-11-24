using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class CreateModel : PageModel
    {
        // Every booking is a fixed 2-hour slot (same rule as the Staff module)
        private static readonly TimeSpan SlotLength = TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;

        private readonly IEmailService _emailService;

        public CreateModel(
    ApplicationDbContext context,
    IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public BookingInputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class BookingInputModel
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
            public int? NumberOfGuests { get; set; } = 2;
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

            // 4. Find the smallest free table that fits the party
            DateTime nextDay = date.AddDays(1);
            TimeSpan start = time;
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
                .Where(t => t.Capacity >= numberOfGuests
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

            // 5. Save the booking and its notification together
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var reservation = new Reservation
            {
                CustomerID = customerId.Value,
                TableID = table.TableID,
                Date = date,
                Time = time,
                NumberOfGuests = numberOfGuests,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Reservations.Add(reservation);

            // First save generates ReservationID.
            await _context.SaveChangesAsync();

            var notification = new Notification
            {
                UserID = customerId.Value,
                UserType = "Customer",
                Message =
                    $"Reservation RES-{reservation.ReservationID:D6} " +
                    "was submitted and is awaiting staff confirmation.",
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            string emailContent =
    $"""
    <p>Hello,</p>

    <p>
        Your reservation request has been submitted
        successfully and is awaiting staff confirmation.
    </p>

    <div style="
        background:#f9fafb;
        border-radius:10px;
        padding:14px;
        margin:18px 0;">

        <strong>Reservation Reference</strong><br/>
        RES-{reservation.ReservationID:D6}
    </div>

    <div style="
        background:#f9fafb;
        border-radius:10px;
        padding:14px;
        margin:18px 0;">

        <strong>Date</strong><br/>
        {date:dd MMM yyyy}

        <br/><br/>

        <strong>Time</strong><br/>
        {DateTime.Today.Add(time):h:mm tt}

        <br/><br/>

        <strong>Guests</strong><br/>
        {numberOfGuests}

        <br/><br/>

        <strong>Table</strong><br/>
        {table.TableNumber}

        <br/><br/>

        <strong>Status</strong><br/>
        Pending
    </div>

    <p>
        You will receive another email once the
        reservation has been confirmed or cancelled.
    </p>

    <p>
        Regards,<br/>
        Dineflow Reservations Team
    </p>
    """;

            await _emailService.SendEmailAsync(
                User.FindFirstValue(ClaimTypes.Email)!,
                "Reservation Submitted",
                EmailTemplates.Wrap(
                    "Reservation Submitted",
                    emailContent));

            string reservationTime =
    DateTime.Today
        .Add(time)
        .ToString("h:mm tt");

            StatusMessage =
                $"Reservation made for table {table.TableNumber} " +
                $"on {date:dd MMM yyyy} at {reservationTime}. " +
                "It is pending confirmation.";

            return RedirectToPage(
                "/Customer/Reservations/Index");
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}