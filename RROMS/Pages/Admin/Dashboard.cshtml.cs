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

        // Summary cards
        public int TotalReservations { get; set; }
        public int TodayReservations { get; set; }
        public int PendingOrders { get; set; }
        public int TotalMenuItems { get; set; }
        public int TotalStaff { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }

        // Chart data (JSON-serialized)
        public string ReservationDatesJson { get; set; } = "[]";
        public string ReservationCountsJson { get; set; } = "[]";
        public string OrderStatusLabelsJson { get; set; } = "[]";
        public string OrderStatusCountsJson { get; set; } = "[]";
        public string RevenueDatesJson { get; set; } = "[]";
        public string RevenueAmountsJson { get; set; } = "[]";
        public string CategoryLabelsJson { get; set; } = "[]";
        public string CategoryCountsJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;

            // ---- Summary cards ----
            TotalReservations = await _db.Reservations.CountAsync();
            TodayReservations = await _db.Reservations.CountAsync(r => r.ReservationDate.Date == today);
            PendingOrders = await _db.Orders.CountAsync(o => o.Status == "Pending");
            TotalMenuItems = await _db.MenuItems.CountAsync();
            TotalStaff = await _db.Staff.CountAsync();
            TotalCustomers = await _db.Customers.CountAsync();
            TotalRevenue = await _db.Orders
                .Where(o => o.Status == "Served")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // ---- Chart 1: Reservations last 14 days ----
            var start14 = today.AddDays(-13);
            var reservations = await _db.Reservations
                .Where(r => r.ReservationDate >= start14)
                .ToListAsync();

            var last14Days = Enumerable.Range(0, 14)
                .Select(i => start14.AddDays(i))
                .ToList();

            var resDates = last14Days.Select(d => d.ToString("MM/dd")).ToList();
            var resCounts = last14Days
                .Select(d => reservations.Count(r => r.ReservationDate.Date == d))
                .ToList();

            ReservationDatesJson = System.Text.Json.JsonSerializer.Serialize(resDates);
            ReservationCountsJson = System.Text.Json.JsonSerializer.Serialize(resCounts);

            // ---- Chart 2: Orders by status ----
            var ordersByStatus = await _db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            OrderStatusLabelsJson = System.Text.Json.JsonSerializer.Serialize(
                ordersByStatus.Select(o => o.Status).ToList());
            OrderStatusCountsJson = System.Text.Json.JsonSerializer.Serialize(
                ordersByStatus.Select(o => o.Count).ToList());

            // ---- Chart 3: Revenue last 7 days ----
            var start7 = today.AddDays(-6);
            var servedOrders = await _db.Orders
                .Where(o => o.Status == "Served" && o.OrderDate >= start7)
                .ToListAsync();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => start7.AddDays(i))
                .ToList();

            var revDates = last7Days.Select(d => d.ToString("MM/dd")).ToList();
            var revAmounts = last7Days
                .Select(d => servedOrders.Where(o => o.OrderDate.Date == d).Sum(o => o.TotalAmount))
                .ToList();

            RevenueDatesJson = System.Text.Json.JsonSerializer.Serialize(revDates);
            RevenueAmountsJson = System.Text.Json.JsonSerializer.Serialize(revAmounts);

            // ---- Chart 4: Menu items per category ----
            var catCounts = await _db.MenuCategories
                .Select(c => new { c.Name, Count = c.Id == 0 ? 0 : _db.MenuItems.Count(m => m.MenuCategoryId == c.Id) })
                .ToListAsync();

            CategoryLabelsJson = System.Text.Json.JsonSerializer.Serialize(
                catCounts.Select(c => c.Name).ToList());
            CategoryCountsJson = System.Text.Json.JsonSerializer.Serialize(
                catCounts.Select(c => c.Count).ToList());
        }
    }
}