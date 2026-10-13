
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Dashboard
{
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

        public List<Reservation> TodayReservationList
        { get; set; } = new();

        public async Task OnGetAsync()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            // Count today's reservations
            TodayReservations = await _context.Reservations
                .CountAsync(r =>
                    r.Date >= today &&
                    r.Date < tomorrow);

            // Count pending customer orders
            PendingOrders = await _context.CustomerOrders
                .CountAsync(o => o.Status == "Pending");

            // Count orders ready for serving
            OrdersReady = await _context.CustomerOrders
                .CountAsync(o => o.Status == "Ready to Serve");

            // Count payments awaiting confirmation
            PendingPayments = await _context.Payments
                .CountAsync(p => p.Status == "Pending");

            // Retrieve today's reservations
            TodayReservationList = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r =>
                    r.Date >= today &&
                    r.Date < tomorrow)
                .OrderBy(r => r.Time)
                .ToListAsync();
        }
    }
}
