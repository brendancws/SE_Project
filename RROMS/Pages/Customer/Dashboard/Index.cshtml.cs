using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Dashboard
{
    // Protected by the CustomerOnly folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CustomerName { get; private set; } = "Customer";

        public Reservation? NextReservation { get; private set; }

        public CustomerOrder? LatestOrder { get; private set; }

        public int UnreadNotificationCount { get; private set; }

        public string BookingReference =>
            NextReservation == null
                ? "-"
                : $"RES-{NextReservation.ReservationID:D6}";

        public string OrderReference =>
            LatestOrder == null
                ? "-"
                : $"ORD-{LatestOrder.OrderID:D6}";

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            RROMS.Models.Customer? customer =
    await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c =>
            c.CustomerID == customerId.Value);

            if (customer == null)
            {
                return Challenge();
            }

            CustomerName =
                string.IsNullOrWhiteSpace(customer.Name)
                    ? User.Identity?.Name ?? "Customer"
                    : customer.Name;

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            TimeSpan currentTime = now.TimeOfDay;

            NextReservation = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Table)
                .Where(r =>
    r.CustomerID == customerId.Value &&
    r.Status != "Cancelled" &&
    (
        r.Date > today ||
        (r.Date == today &&
         r.Time >= currentTime)
    ))
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Time)
                .FirstOrDefaultAsync();

            LatestOrder = await _context.CustomerOrders
                .AsNoTracking()
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .Where(o =>
                    o.CustomerID == customerId.Value)
                .OrderByDescending(o => o.OrderDate)
                .ThenByDescending(o => o.OrderID)
                .FirstOrDefaultAsync();

            UnreadNotificationCount =
                await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(n =>
                        n.UserID == customerId.Value &&
                        n.UserType == "Customer" &&
                        n.IsRead != true);

            return Page();
        }

        public static string ReservationBadgeClass(string? status)
        {
            return (status ?? string.Empty).ToLowerInvariant() switch
            {
                "confirmed" => "bg-success",
                "pending" => "bg-warning text-dark",
                "cancelled" => "bg-danger",
                _ => "bg-secondary"
            };
        }

        public static string OrderBadgeClass(string? status)
        {
            return (status ?? string.Empty).ToLowerInvariant() switch
            {
                "pending" => "bg-warning text-dark",
                "preparing" => "bg-info text-dark",
                "ready to serve" => "bg-primary",
                "served" => "bg-success",
                "completed" => "bg-dark",
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