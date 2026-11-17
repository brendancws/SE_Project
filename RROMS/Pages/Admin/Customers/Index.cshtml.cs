using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using CustomerEntity = RROMS.Models.Customer;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private static readonly HashSet<string> AllowedStatusFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "active",
                "inactive"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<CustomerEntity> Customers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalResults == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            Math.Min(
                PageNumber * PageSize,
                TotalResults);

        public async Task OnGetAsync()
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            Status = string.IsNullOrWhiteSpace(Status)
                ? "all"
                : Status.Trim().ToLowerInvariant();

            if (!AllowedStatusFilters.Contains(Status))
            {
                Status = "all";
            }

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            ActiveCount = await _db.Customers
                .AsNoTracking()
                .CountAsync(customer =>
                    customer.Status == "Active");

            InactiveCount = await _db.Customers
                .AsNoTracking()
                .CountAsync(customer =>
                    customer.Status != "Active");

            var query = _db.Customers
                .AsNoTracking()
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(customer =>
                    customer.Name.Contains(Search) ||
                    customer.Email.Contains(Search) ||
                    (
                        customer.Phone != null &&
                        customer.Phone.Contains(Search)
                    ));
            }

            query = Status switch
            {
                "active" => query.Where(customer =>
                    customer.Status == "Active"),

                "inactive" => query.Where(customer =>
                    customer.Status != "Active"),

                _ => query
            };

            TotalResults = await query.CountAsync();

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalResults / (double)PageSize));

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Customers = await query
                .OrderBy(customer => customer.Name)
                .ThenBy(customer => customer.Email)
                .ThenBy(customer => customer.CustomerID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}