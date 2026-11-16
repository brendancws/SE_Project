using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Payments
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private static readonly HashSet<string> AllowedFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "paid",
                "pending",
                "failed",
                "refunded"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Payment> Payments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public decimal PaidTotal { get; set; }

        public decimal PendingTotal { get; set; }

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

            var query = _db.Payments
                .AsNoTracking()
                .Include(payment => payment.Order)
                .AsQueryable();

            query = Filter switch
            {
                "paid" => query.Where(payment =>
                    payment.Status == "Paid"),

                "pending" => query.Where(payment =>
                    payment.Status == "Pending"),

                "failed" => query.Where(payment =>
                    payment.Status == "Failed"),

                "refunded" => query.Where(payment =>
                    payment.Status == "Refunded"),

                _ => query
            };

            if (Search != null)
            {
                string numberSearch = Search.TrimStart('#');

                bool isNumber =
                    int.TryParse(numberSearch, out int numericId);

                query = query.Where(payment =>
                    (
                        isNumber &&
                        (
                            payment.PaymentID == numericId ||
                            payment.OrderID == numericId
                        )
                    ) ||
                    (
                        payment.TransactionNo != null &&
                        payment.TransactionNo.Contains(Search)
                    ) ||
                    (
                        payment.Method != null &&
                        payment.Method.Contains(Search)
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

            Payments = await query
                .OrderByDescending(payment => payment.PaymentDate)
                .ThenByDescending(payment => payment.PaymentID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            PaidTotal = await _db.Payments
                .AsNoTracking()
                .Where(payment => payment.Status == "Paid")
                .SumAsync(payment => (decimal?)payment.Amount)
                ?? 0m;

            PendingTotal = await _db.Payments
                .AsNoTracking()
                .Where(payment => payment.Status == "Pending")
                .SumAsync(payment => (decimal?)payment.Amount)
                ?? 0m;
        }
    }
}