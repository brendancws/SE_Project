using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Payments
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
                "Paid",
                "Failed",
                "Cancelled"
            };

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Payment> Payments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int TotalPayments { get; set; }

        public int PendingCount { get; set; }

        public int PaidCount { get; set; }

        public int FailedOrCancelledCount { get; set; }

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

            TotalPayments = await _context.Payments
                .AsNoTracking()
                .CountAsync();

            PendingCount = await _context.Payments
                .AsNoTracking()
                .CountAsync(payment =>
                    payment.Status == "Pending");

            PaidCount = await _context.Payments
                .AsNoTracking()
                .CountAsync(payment =>
                    payment.Status == "Paid");

            FailedOrCancelledCount = await _context.Payments
                .AsNoTracking()
                .CountAsync(payment =>
                    payment.Status == "Failed" ||
                    payment.Status == "Cancelled");

            var query = _context.Payments
                .AsNoTracking()
                .Include(payment => payment.Order)
                    .ThenInclude(order => order!.Customer)
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(payment =>
                    payment.Order != null &&
                    payment.Order.Customer != null &&
                    (
                        payment.Order.Customer.Name.Contains(Search) ||
                        (
                            payment.Order.Customer.Phone != null &&
                            payment.Order.Customer.Phone.Contains(Search)
                        )
                    ));
            }

            if (!string.Equals(
                StatusFilter,
                "All",
                StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(payment =>
                    payment.Status == StatusFilter);
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

            Payments = await query
                .OrderByDescending(payment => payment.PaymentDate)
                .ThenByDescending(payment => payment.PaymentID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}