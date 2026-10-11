using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DashboardModel(ApplicationDbContext db) => _db = db;

        public int TotalReservations { get; set; }
        public int TodayReservations { get; set; }
        public int PendingOrders { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalStaff { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;

            TotalReservations = await _db.Reservations.CountAsync();
            TodayReservations = await _db.Reservations.CountAsync(r => r.ReservationDate.Date == today);
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == "Pending");
            TotalMenuItems = await _db.MenuItems.CountAsync();
            TotalStaff = await _db.Staff.CountAsync();
            TotalCustomers = await _db.Customers.CountAsync();
            TotalRevenue = await _db.Orders
                .Where(o => o.Status == "Served")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        }
    }
}