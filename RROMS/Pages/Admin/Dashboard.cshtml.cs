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

        public DashboardModel(ApplicationDbContext db)
        {
            _db = db;
        }

        // Summary cards
        public int TotalReservations { get; set; }

        public int TodayReservations { get; set; }

        public int PendingOrders { get; set; }

        public int TotalMenuItems { get; set; }

        public int TotalStaff { get; set; }

        public int TotalCustomers { get; set; }

        public decimal TotalRevenue { get; set; }

        // Chart data
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
            var tomorrow = today.AddDays(1);

            // Summary cards
            TotalReservations = await _db.Reservations
                .AsNoTracking()
                .CountAsync();

            TodayReservations = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow);

            PendingOrders = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Pending");

            TotalMenuItems = await _db.MenuItems
                .AsNoTracking()
                .CountAsync();

            TotalStaff = await _db.Staff
                .AsNoTracking()
                .CountAsync();

            TotalCustomers = await _db.Customers
                .AsNoTracking()
                .CountAsync();

            TotalRevenue = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.Status == "Served")
                .SumAsync(order =>
                    order.TotalAmount ?? 0m);

            // Chart 1: Reservations during the last 14 days
            var start14 = today.AddDays(-13);

            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Date >= start14 &&
                    reservation.Date < tomorrow)
                .Select(reservation => reservation.Date)
                .ToListAsync();

            var last14Days = Enumerable.Range(0, 14)
                .Select(index =>
                    start14.AddDays(index))
                .ToList();

            var reservationDateLabels = last14Days
                .Select(date =>
                    date.ToString("MM/dd"))
                .ToList();

            var reservationCounts = last14Days
                .Select(date =>
                    reservations.Count(
                        reservationDate =>
                            reservationDate.Date == date.Date))
                .ToList();

            ReservationDatesJson =
                System.Text.Json.JsonSerializer.Serialize(
                    reservationDateLabels);

            ReservationCountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    reservationCounts);

            // Chart 2: Orders grouped by status
            var ordersByStatus = await _db.CustomerOrders
                .AsNoTracking()
                .GroupBy(order =>
                    order.Status ?? "Unknown")
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count()
                })
                .OrderBy(result => result.Status)
                .ToListAsync();

            OrderStatusLabelsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    ordersByStatus
                        .Select(result => result.Status)
                        .ToList());

            OrderStatusCountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    ordersByStatus
                        .Select(result => result.Count)
                        .ToList());

            // Chart 3: Served-order revenue during the last 7 days
            var start7 = today.AddDays(-6);

            var servedOrders = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.Status == "Served" &&
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value >= start7 &&
                    order.OrderDate.Value < tomorrow)
                .Select(order => new
                {
                    OrderDate = order.OrderDate!.Value,
                    Amount = order.TotalAmount ?? 0m
                })
                .ToListAsync();

            var last7Days = Enumerable.Range(0, 7)
                .Select(index =>
                    start7.AddDays(index))
                .ToList();

            var revenueDateLabels = last7Days
                .Select(date =>
                    date.ToString("MM/dd"))
                .ToList();

            var revenueAmounts = last7Days
                .Select(date =>
                    servedOrders
                        .Where(order =>
                            order.OrderDate.Date == date.Date)
                        .Sum(order => order.Amount))
                .ToList();

            RevenueDatesJson =
                System.Text.Json.JsonSerializer.Serialize(
                    revenueDateLabels);

            RevenueAmountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    revenueAmounts);

            // Chart 4: Menu items grouped by category
            // Use mapped fields only:
            // MenuCategory.CategoryID
            // MenuCategory.CategoryName
            // MenuItem.CategoryID
            var categoryCounts = await _db.MenuCategories
                .AsNoTracking()
                .GroupJoin(
                    _db.MenuItems.AsNoTracking(),
                    category => category.CategoryID,
                    menuItem => menuItem.CategoryID,
                    (category, menuItems) => new
                    {
                        Name = category.CategoryName,
                        Count = menuItems.Count()
                    })
                .OrderBy(result => result.Name)
                .ToListAsync();

            CategoryLabelsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    categoryCounts
                        .Select(result => result.Name)
                        .ToList());

            CategoryCountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    categoryCounts
                        .Select(result => result.Count)
                        .ToList());
        }
    }
}