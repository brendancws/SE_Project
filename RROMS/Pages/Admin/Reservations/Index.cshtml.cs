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

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Reservation> Reservations { get; set; } = new();

        public string Filter { get; set; } = "all";

        public int TodayCount { get; set; }

        public int PendingCount { get; set; }

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = string.IsNullOrWhiteSpace(filter)
                ? "all"
                : filter.ToLowerInvariant();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var query = _db.Reservations
                .AsNoTracking()
                .Include(reservation => reservation.Customer)
                .Include(reservation => reservation.Table)
                .AsQueryable();

            if (Filter == "today")
            {
                query = query.Where(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow);
            }
            else if (Filter == "upcoming")
            {
                query = query.Where(reservation =>
                    reservation.Date >= today);
            }
            else if (Filter == "pending")
            {
                query = query.Where(reservation =>
                    reservation.Status == "Pending");
            }
            else if (Filter == "confirmed")
            {
                query = query.Where(reservation =>
                    reservation.Status == "Confirmed");
            }
            else if (Filter == "cancelled")
            {
                query = query.Where(reservation =>
                    reservation.Status == "Cancelled");
            }

            Reservations = await query
                .OrderBy(reservation => reservation.Date)
                .ThenBy(reservation => reservation.Time)
                .ToListAsync();

            TodayCount = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow);

            PendingCount = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.Status == "Pending");
        }
    }
}