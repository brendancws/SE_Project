using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Reports
{
    [Authorize(Roles = "Admin")]
    public class ReservationsModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public ReservationsModel(ApplicationDbContext db) => _db = db;

        public List<Reservation> Results { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)] public DateTime? From { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? To { get; set; }
        [BindProperty(SupportsGet = true)] public string Status { get; set; } = "all";

        // Summary
        public int TotalCount { get; set; }
        public int TotalGuests { get; set; }
        public int PendingCount { get; set; }
        public int ConfirmedCount { get; set; }
        public int CancelledCount { get; set; }
        public int CompletedCount { get; set; }

        public async Task OnGetAsync()
        {
            // Default date range = last 30 days if not set
            if (!From.HasValue) From = DateTime.Today.AddDays(-30);
            if (!To.HasValue) To = DateTime.Today.AddDays(1);

            var fromDate = From.Value.Date;
            var toDate = To.Value.Date.AddDays(1); // end of day

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
    }
}