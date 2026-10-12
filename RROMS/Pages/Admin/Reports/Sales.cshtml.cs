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
        public SalesModel(ApplicationDbContext db) => _db = db;

        public List<Order> Results { get; set; } = new();

        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string Status { get; set; } = "all";

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

            var sb = new StringBuilder();
            sb.AppendLine("Order #,Date,Customer,Total (RM),Status");

            foreach (var o in Results)
            {
                sb.AppendLine(string.Join(",",
                    o.Id,
                    o.OrderDate.ToString("yyyy-MM-dd HH:mm"),
                    Csv(o.CustomerName),
                    o.TotalAmount.ToString("F2"),
                    o.Status
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"sales_{DateTime.Now:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private async Task LoadAsync()
        {
            if (!From.HasValue) From = DateTime.Today.AddDays(-30);
            if (!To.HasValue) To = DateTime.Today.AddDays(1);

            var fromDate = From.Value.Date;
            var toDate = To.Value.Date.AddDays(1);

            var query = _db.Orders
                .Where(o => o.OrderDate >= fromDate && o.OrderDate < toDate);

            if (Status != "all" && !string.IsNullOrEmpty(Status))
                query = query.Where(o => o.Status == Status);

            Results = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            TotalOrders = Results.Count;
            TotalRevenue = Results.Sum(o => o.TotalAmount);
            AverageOrder = TotalOrders > 0 ? TotalRevenue / TotalOrders : 0;
            ServedRevenue = Results.Where(o => o.Status == "Served").Sum(o => o.TotalAmount);
            PendingRevenue = Results.Where(o => o.Status == "Pending").Sum(o => o.TotalAmount);
        }

        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}