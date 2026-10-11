using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Reservations
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<Reservation> Reservations { get; set; } = new();
        public string Filter { get; set; } = "all";
        public int TodayCount { get; set; }
        public int PendingCount { get; set; }

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = filter;

            var query = _db.Reservations
                .Include(r => r.Table)
                .AsQueryable();

            var today = DateTime.Today;

            if (filter == "today")
                query = query.Where(r => r.ReservationDate.Date == today);
            else if (filter == "upcoming")
                query = query.Where(r => r.ReservationDate.Date >= today);
            else if (filter == "pending")
                query = query.Where(r => r.Status == "Pending");
            else if (filter == "cancelled")
                query = query.Where(r => r.Status == "Cancelled");

            Reservations = await query.OrderBy(r => r.ReservationDate).ToListAsync();

            TodayCount = await _db.Reservations.CountAsync(r => r.ReservationDate.Date == today);
            PendingCount = await _db.Reservations.CountAsync(r => r.Status == "Pending");
        }
    }
}