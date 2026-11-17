using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Orders
{
    [Authorize(Roles = "Staff")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private static readonly HashSet<string> AllowedStatuses =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "All",
                "Pending",
                "Preparing",
                "Ready to Serve",
                "Served",
                "Completed",
                "Cancelled"
            };

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CustomerOrder> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int TotalOrders { get; set; }

        public int PreparingCount { get; set; }

        public int ReadyCount { get; set; }

        public int CompletedCount { get; set; }

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
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            StatusFilter = string.IsNullOrWhiteSpace(StatusFilter)
                ? "All"
                : StatusFilter.Trim();

            if (!AllowedStatuses.Contains(StatusFilter))
            {
                StatusFilter = "All";
            }

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            TotalOrders = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync();

            PreparingCount = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Preparing");

            ReadyCount = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Ready to Serve");

            CompletedCount = await _context.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.Status == "Completed");

            var query = _context.CustomerOrders
                .AsNoTracking()
                .Include(order => order.Customer)
                .Include(order => order.Reservation)
                    .ThenInclude(reservation => reservation!.Table)
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(order =>
                    order.Customer != null &&
                    (
                        order.Customer.Name.Contains(Search) ||
                        (
                            order.Customer.Phone != null &&
                            order.Customer.Phone.Contains(Search)
                        )
                    ));
            }

            if (!string.Equals(
                StatusFilter,
                "All",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(order =>
                    order.Status == StatusFilter);
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
        }
    }
}