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

        // Main operational summary
        public int TotalReservations { get; set; }

        public int TodayReservations { get; set; }

        public int TodayGuests { get; set; }

        public int PendingOrders { get; set; }

        public int PreparingOrders { get; set; }

        public int ReadyOrders { get; set; }

        public int TotalTables { get; set; }

        public int AvailableTables { get; set; }

        public int TotalMenuItems { get; set; }

        public int UnavailableMenuItems { get; set; }

        public int TotalStaff { get; set; }

        public int TotalCustomers { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal TodayRevenue { get; set; }

        // Live operational panels
        public List<UpcomingReservationRow> UpcomingReservations
        { get; set; } = new();

        public List<ActiveOrderRow> ActiveOrders
        { get; set; } = new();

        // Chart data
        public string ReservationDatesJson { get; set; } = "[]";

        public string ReservationCountsJson { get; set; } = "[]";

        public string OrderStatusLabelsJson { get; set; } = "[]";

        public string OrderStatusCountsJson { get; set; } = "[]";

        public string RevenueDatesJson { get; set; } = "[]";

        public string RevenueAmountsJson { get; set; } = "[]";

        public string CategoryLabelsJson { get; set; } = "[]";

        public string CategoryCountsJson { get; set; } = "[]";

        public class UpcomingReservationRow
        {
            public int ReservationID { get; set; }

            public TimeSpan Time { get; set; }

            public string CustomerName { get; set; }
                = string.Empty;

            public int NumberOfGuests { get; set; }

            public string TableNumber { get; set; } = "—";

            public string Status { get; set; } = "Pending";
        }

        public class ActiveOrderRow
        {
            public int OrderID { get; set; }

            public string CustomerName { get; set; }
                = string.Empty;

            public decimal TotalAmount { get; set; }

            public string Status { get; set; } = "Pending";

            public DateTime? OrderDate { get; set; }
        }

        public async Task OnGetAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var currentTime = DateTime.Now.TimeOfDay;

            // -------------------------------------------------
            // Main operational summary
            // -------------------------------------------------

            TotalReservations = await _db.Reservations
                .AsNoTracking()
                .CountAsync();

            TodayReservations = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow);

            TodayGuests = await _db.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow &&
                    reservation.Status != "Cancelled")
                .SumAsync(reservation =>
                    (int?)reservation.NumberOfGuests) ?? 0;

            PendingOrders = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Pending");

            PreparingOrders = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Preparing");

            ReadyOrders = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Ready to Serve");

            TotalTables = await _db.RestaurantTables
                .AsNoTracking()
                .CountAsync();

            AvailableTables = await _db.RestaurantTables
                .AsNoTracking()
                .CountAsync(table =>
                    table.Status == "Available");

            TotalMenuItems = await _db.MenuItems
                .AsNoTracking()
                .CountAsync();

            UnavailableMenuItems = await _db.MenuItems
                .AsNoTracking()
                .CountAsync(menuItem =>
                    menuItem.Availability != true);

            TotalStaff = await _db.Staff
                .AsNoTracking()
                .CountAsync();

            TotalCustomers = await _db.Customers
                .AsNoTracking()
                .CountAsync();

            TotalRevenue = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
    order.Status == "Completed")
                .SumAsync(order =>
                    order.TotalAmount ?? 0m);

            TodayRevenue = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.Status == "Completed" &&
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value >= today &&
                    order.OrderDate.Value < tomorrow)
                .SumAsync(order =>
                    order.TotalAmount ?? 0m);

            // -------------------------------------------------
            // Today's upcoming reservation schedule
            // -------------------------------------------------

            UpcomingReservations = await _db.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow &&
                    reservation.Status != "Cancelled" &&
                    reservation.Time >= currentTime)
                .OrderBy(reservation =>
                    reservation.Time)
                .ThenBy(reservation =>
                    reservation.ReservationID)
                .Take(5)
                .Select(reservation =>
                    new UpcomingReservationRow
                    {
                        ReservationID =
                            reservation.ReservationID,

                        Time =
                            reservation.Time,

                        CustomerName =
    reservation.Customer != null
        ? reservation.Customer.Name
        : "Unknown customer",

                        NumberOfGuests =
                            reservation.NumberOfGuests,

                        TableNumber =
                            reservation.Table != null
                                ? reservation.Table.TableNumber
                                : "—",

                        Status =
                            reservation.Status ?? "Pending"
                    })
                .ToListAsync();

            // -------------------------------------------------
            // Oldest active order queue
            // -------------------------------------------------

            ActiveOrders = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.Status == "Pending" ||
                    order.Status == "Preparing" ||
                    order.Status == "Ready to Serve")
                .OrderBy(order =>
                    order.OrderDate)
                .ThenBy(order =>
                    order.OrderID)
                .Take(5)
                .Select(order =>
                    new ActiveOrderRow
                    {
                        OrderID =
                            order.OrderID,

                        CustomerName =
    order.Customer != null
        ? order.Customer.Name
        : "Unknown customer",

                        TotalAmount =
                            order.TotalAmount ?? 0m,

                        Status =
                            order.Status ?? "Pending",

                        OrderDate =
                            order.OrderDate
                    })
                .ToListAsync();

            // -------------------------------------------------
            // Chart 1: Reservations during the last 14 days
            // -------------------------------------------------

            var start14 = today.AddDays(-13);

            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.Date >= start14 &&
                    reservation.Date < tomorrow)
                .Select(reservation =>
                    reservation.Date)
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

            // -------------------------------------------------
            // Chart 2: Orders grouped by status
            // -------------------------------------------------

            var ordersByStatus = await _db.CustomerOrders
                .AsNoTracking()
                .GroupBy(order =>
                    order.Status ?? "Unknown")
                .Select(group =>
                    new
                    {
                        Status = group.Key,
                        Count = group.Count()
                    })
                .OrderBy(result =>
                    result.Status)
                .ToListAsync();

            OrderStatusLabelsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    ordersByStatus
                        .Select(result =>
                            result.Status)
                        .ToList());

            OrderStatusCountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    ordersByStatus
                        .Select(result =>
                            result.Count)
                        .ToList());

            // -------------------------------------------------
            // Chart 3: Completed revenue during the last 7 days
            // -------------------------------------------------

            var start7 = today.AddDays(-6);

            var completedOrders = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.Status == "Completed" &&
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value >= start7 &&
                    order.OrderDate.Value < tomorrow)
                .Select(order =>
                    new
                    {
                        OrderDate =
                            order.OrderDate!.Value,

                        Amount =
                            order.TotalAmount ?? 0m
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
                    completedOrders
                        .Where(order =>
                            order.OrderDate.Date == date.Date)
                        .Sum(order =>
                            order.Amount))
                .ToList();

            RevenueDatesJson =
                System.Text.Json.JsonSerializer.Serialize(
                    revenueDateLabels);

            RevenueAmountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    revenueAmounts);

            // -------------------------------------------------
            // Chart 4: Menu items grouped by category
            // -------------------------------------------------

            var categoryCounts = await _db.MenuCategories
                .AsNoTracking()
                .GroupJoin(
                    _db.MenuItems.AsNoTracking(),

                    category =>
                        category.CategoryID,

                    menuItem =>
                        menuItem.CategoryID,

                    (category, menuItems) =>
                        new
                        {
                            Name =
                                category.CategoryName,

                            Count =
                                menuItems.Count()
                        })
                .OrderBy(result =>
                    result.Name)
                .ToListAsync();

            CategoryLabelsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    categoryCounts
                        .Select(result =>
                            result.Name)
                        .ToList());

            CategoryCountsJson =
                System.Text.Json.JsonSerializer.Serialize(
                    categoryCounts
                        .Select(result =>
                            result.Count)
                        .ToList());
        }
    }
}