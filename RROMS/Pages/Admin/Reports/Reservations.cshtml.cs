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
    public class ReservationsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ReservationsModel(ApplicationDbContext db) => _db = db;

        public List<Reservation> Results { get; set; } = new();

        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string Status { get; set; } = "all";

        public int TotalCount { get; set; }
        public int TotalGuests { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
        public int CompletedCount { get; set; }

        public async Task OnGetAsync()
        {
            await LoadAsync();
        }

        // Export to CSV handler (called via ?handler=Export)
        public async Task<IActionResult> OnGetExportAsync()
        {
            await LoadAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Id,Date,Customer,Phone,Party Size,Table,Status");

            foreach (var r in Results)
            {
                sb.AppendLine(string.Join(",",
                    r.Id,
                    r.ReservationDate.ToString("yyyy-MM-dd HH:mm"),
                    Csv(r.CustomerName),
                    Csv(r.CustomerPhone ?? ""),
                    r.PartySize,
                    Csv(r.Table?.TableNumber ?? ""),
                    r.Status
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"reservations_{DateTime.Now:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private async Task LoadAsync()
        {
            if (!From.HasValue) From = DateTime.Today.AddDays(-30);
            if (!To.HasValue) To = DateTime.Today.AddDays(1);

            var fromDate = From.Value.Date;
            var toDate = To.Value.Date.AddDays(1);

            var query = _db.Reservations
                .Include(r => r.Table)
                .Where(r => r.ReservationDate >= fromDate && r.ReservationDate < toDate);

            if (Status != "all" && !string.IsNullOrEmpty(Status))
                query = query.Where(r => r.Status == Status);

            Results = await query.OrderBy(r => r.ReservationDate).ToListAsync();

            TotalCount = Results.Count;
            TotalGuests = Results.Sum(r => r.PartySize);
            PendingCount = Results.Count(r => r.Status == "Pending");
            ConfirmedCount = Results.Count(r => r.Status == "Confirmed");
            CancelledCount = Results.Count(r => r.Status == "Cancelled");
            CompletedCount = Results.Count(r => r.Status == "Completed");
        }

        // Escape commas/quotes in CSV
        private static string Csv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}