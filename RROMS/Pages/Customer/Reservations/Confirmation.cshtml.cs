using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Reservations
{
    // Protected by the CustomerOnly folder convention in Program.cs
    public class ConfirmationModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ConfirmationModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Reservation? Reservation { get; private set; }

        public string BookingReference =>
            Reservation == null
                ? "-"
                : $"RES-{Reservation.ReservationID:D6}";

        public string CustomerName =>
            Reservation?.Customer?.Name
            ?? User.Identity?.Name
            ?? "Customer";

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            Reservation = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r =>
                    r.ReservationID == id &&
                    r.CustomerID == customerId.Value);

            // Do not reveal whether another customer's reservation exists
            if (Reservation == null)
            {
                return NotFound();
            }

            return Page();
        }

        public static string StatusBadgeClass(string? status)
        {
            return (status ?? string.Empty).ToLowerInvariant() switch
            {
                "confirmed" => "bg-success",
                "pending" => "bg-warning text-dark",
                "cancelled" => "bg-danger",
                _ => "bg-secondary"
            };
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(idValue, out int id) && id > 0
                ? id
                : null;
        }
    }
}