using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using System.Text;

namespace RROMS.Pages.Admin.Reports
{
    [Authorize(Roles = "Admin")]
    public class SalesModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public SalesModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<CustomerOrder> Results { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "all";

        public int TotalOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AverageOrder { get; set; }

        public decimal ServedRevenue { get; set; }

        public decimal PendingRevenue { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();

            var csv = new StringBuilder();

            csv.AppendLine(
                "Order #,Date,Customer,Total (RM),Status");

            foreach (var order in Results)
            {
                csv.AppendLine(string.Join(",",
                    order.OrderID,
                    order.OrderDate?.ToString(
                        "yyyy-MM-dd HH:mm") ?? string.Empty,
                    Csv(order.Customer?.Name ?? string.Empty),
                    (order.TotalAmount ?? 0m).ToString("F2"),
                    Csv(order.Status ?? string.Empty)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());

            var fileName =
                $"sales_{DateTime.Now:yyyy-MM-dd}.csv";

            return File(
                bytes,
                "text/csv",
                fileName);
        }

        private async Task LoadAsync()
        {
            From ??= DateTime.Today.AddDays(-30);
            To ??= DateTime.Today;

            var fromDate = From.Value.Date;

            // The end date is inclusive in the interface.
            var toDateExclusive = To.Value.Date.AddDays(1);

            var query = _db.CustomerOrders
                .AsNoTracking()
                .Include(order => order.Customer)
                .Where(order =>
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value >= fromDate &&
                    order.OrderDate.Value < toDateExclusive);

            if (!string.IsNullOrWhiteSpace(Status) &&
                !string.Equals(
                    Status,
                    "all",
                    StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(order =>
                    order.Status == Status);
            }

            Results = await query
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderID)
                .ToListAsync();

            TotalOrders = Results.Count;

            TotalRevenue = Results
                .Sum(order => order.TotalAmount ?? 0m);

            AverageOrder = TotalOrders > 0
                ? TotalRevenue / TotalOrders
                : 0m;

            ServedRevenue = Results
                .Where(order => order.Status == "Served")
                .Sum(order => order.TotalAmount ?? 0m);

            PendingRevenue = Results
                .Where(order => order.Status == "Pending")
                .Sum(order => order.TotalAmount ?? 0m);
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(',') ||
                value.Contains('"') ||
                value.Contains('\n') ||
                value.Contains('\r'))
            {
                return "\"" +
                    value.Replace("\"", "\"\"") +
                    "\"";
            }

            return value;
        }
    }
}