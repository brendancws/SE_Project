using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Dashboard
{
    [Authorize(Roles = "Staff")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TodayReservations { get; set; }

        public int PendingOrders { get; set; }

        public int OrdersReady { get; set; }

        public int PendingPayments { get; set; }

        public List<Reservation> TodayReservationList { get; set; }
            = new();

        public async Task OnGetAsync()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            TodayReservations = await _context.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow);

            PendingOrders = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Pending");

            OrdersReady = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Ready to Serve");

            PendingPayments = await _context.Payments
                .AsNoTracking()
                .CountAsync(payment =>
                    payment.Status == "Pending");

            TodayReservationList = await _context.Reservations
                .AsNoTracking()
                .Include(reservation =>
                    reservation.Customer)
                .Include(reservation =>
                    reservation.Table)
                .Where(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow)
                .OrderBy(reservation =>
                    reservation.Time)
                .ThenBy(reservation =>
                    reservation.ReservationID)
                .Take(5)
                .ToListAsync();
        }
    }
}