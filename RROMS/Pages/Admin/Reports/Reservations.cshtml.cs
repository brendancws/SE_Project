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

            foreach (var reservation in Results)
            {
                var reservationDateTime =
                    reservation.Date.Date.Add(reservation.Time);

                sb.AppendLine(string.Join(",",
                    reservation.ReservationID,
                    reservationDateTime.ToString("yyyy-MM-dd HH:mm"),
                    Csv(reservation.Customer?.Name ?? string.Empty),
                    Csv(reservation.Customer?.Phone ?? string.Empty),
                    reservation.NumberOfGuests,
                    Csv(reservation.Table?.TableNumber ?? string.Empty),
                    Csv(reservation.Status ?? string.Empty)
                ));
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"reservations_{DateTime.Now:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private async Task LoadAsync()
        {
            From ??= DateTime.Today.AddDays(-30);
            To ??= DateTime.Today;

            var fromDate = From.Value.Date;

            // Makes the selected "To" date inclusive.
            var toDateExclusive = To.Value.Date.AddDays(1);

            var query = _db.Reservations
                .AsNoTracking()
                .Include(reservation => reservation.Customer)
                .Include(reservation => reservation.Table)
                .Where(reservation =>
                    reservation.Date >= fromDate &&
                    reservation.Date < toDateExclusive);

            if (!string.IsNullOrWhiteSpace(Status) &&
                !string.Equals(
                    Status,
                    "all",
                    StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(reservation =>
                    reservation.Status == Status);
            }

            Results = await query
                .OrderBy(reservation => reservation.Date)
                .ThenBy(reservation => reservation.Time)
                .ToListAsync();

            TotalCount = Results.Count;

            TotalGuests = Results.Sum(reservation =>
                reservation.NumberOfGuests);

            PendingCount = Results.Count(reservation =>
                reservation.Status == "Pending");

            ConfirmedCount = Results.Count(reservation =>
                reservation.Status == "Confirmed");

            CancelledCount = Results.Count(reservation =>
                reservation.Status == "Cancelled");

            CompletedCount = Results.Count(reservation =>
                reservation.Status == "Completed");
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