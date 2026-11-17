using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Customers
{
    [Authorize(Roles = "Staff")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public RROMS.Models.Customer Customer { get; set; }
            = null!;

        // Keep these names compatible with the existing Razor page.
        // Only the five most recent records are loaded.
        public List<CustomerOrder> Orders { get; set; }
            = new();

        public List<Reservation> Reservations { get; set; }
            = new();

        public int TotalOrderCount { get; set; }

        public int TotalReservationCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (!id.HasValue)
            {
                return NotFound();
            }

            int customerId = id.Value;

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.CustomerID == customerId);

            if (customer == null)
            {
                return NotFound();
            }

            Customer = customer;

            TotalReservationCount =
                await _context.Reservations
                    .AsNoTracking()
                    .CountAsync(reservation =>
                        reservation.CustomerID ==
                        customerId);

            TotalOrderCount =
                await _context.CustomerOrders
                    .AsNoTracking()
                    .CountAsync(order =>
                        order.CustomerID ==
                        customerId);

            Reservations =
                await _context.Reservations
                    .AsNoTracking()
                    .Include(reservation =>
                        reservation.Table)
                    .Where(reservation =>
                        reservation.CustomerID ==
                        customerId)
                    .OrderByDescending(reservation =>
                        reservation.Date)
                    .ThenByDescending(reservation =>
                        reservation.Time)
                    .ThenByDescending(reservation =>
                        reservation.ReservationID)
                    .Take(5)
                    .ToListAsync();

            Orders =
                await _context.CustomerOrders
                    .AsNoTracking()
                    .Where(order =>
                        order.CustomerID ==
                        customerId)
                    .OrderByDescending(order =>
                        order.OrderDate)
                    .ThenByDescending(order =>
                        order.OrderID)
                    .Take(5)
                    .ToListAsync();

            return Page();
        }
    }
}