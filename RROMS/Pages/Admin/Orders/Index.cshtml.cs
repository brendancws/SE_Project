using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private static readonly HashSet<string> AllowedFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "today",
                "pending",
                "preparing",
                "ready",
                "completed",
                "served",
                "cancelled"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<CustomerOrder> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public decimal TotalRevenue { get; set; }

        public int PendingCount { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalResults == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            Math.Min(PageNumber * PageSize, TotalResults);

        public async Task OnGetAsync()
        {
            Filter = string.IsNullOrWhiteSpace(Filter)
                ? "all"
                : Filter.Trim().ToLowerInvariant();

            if (!AllowedFilters.Contains(Filter))
            {
                Filter = "all";
            }

            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var query = _db.CustomerOrders
                .AsNoTracking()
                .Include(order => order.Customer)
                .AsQueryable();

            query = Filter switch
            {
                "today" => query.Where(order =>
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value >= today &&
                    order.OrderDate.Value < tomorrow),

                "pending" => query.Where(order =>
                    order.Status == "Pending"),

                "preparing" => query.Where(order =>
                    order.Status == "Preparing"),

                "ready" => query.Where(order =>
                    order.Status == "Ready" ||
                    order.Status == "Ready to Serve"),

                "completed" => query.Where(order =>
                    order.Status == "Completed"),

                "served" => query.Where(order =>
                    order.Status == "Served"),

                "cancelled" => query.Where(order =>
                    order.Status == "Cancelled"),

                _ => query
            };

            if (Search != null)
            {
                bool isOrderNumber =
                    int.TryParse(Search.TrimStart('#'), out int orderId);

                query = query.Where(order =>
                    (
                        order.Customer != null &&
                        (
                            order.Customer.Name.Contains(Search) ||
                            order.Customer.Email.Contains(Search)
                        )
                    ) ||
                    (
                        isOrderNumber &&
                        order.OrderID == orderId
                    ));
            }

            TotalResults = await query.CountAsync();

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalResults / (double)PageSize));

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Orders = await query
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            TotalRevenue = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order => order.Status == "Served")
                .SumAsync(order => order.TotalAmount ?? 0m);

            PendingCount = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order => order.Status == "Pending");
        }
    }
}