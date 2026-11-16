using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        private static readonly HashSet<string> AllowedFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "today",
                "upcoming",
                "pending",
                "confirmed",
                "completed",
                "cancelled"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<Reservation> Reservations { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int TodayCount { get; set; }

        public int PendingCount { get; set; }

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

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var query = _db.Reservations
                .AsNoTracking()
                .Include(reservation => reservation.Customer)
                .Include(reservation => reservation.Table)
                .AsQueryable();

            query = Filter switch
            {
                "today" => query.Where(reservation =>
                    reservation.Date >= today &&
                    reservation.Date < tomorrow),

                "upcoming" => query.Where(reservation =>
                    reservation.Date >= today),

                "pending" => query.Where(reservation =>
                    reservation.Status == "Pending"),

                "confirmed" => query.Where(reservation =>
                    reservation.Status == "Confirmed"),

                "completed" => query.Where(reservation =>
                    reservation.Status == "Completed"),

                "cancelled" => query.Where(reservation =>
                    reservation.Status == "Cancelled"),

                _ => query
            };

            if (Search != null)
            {
                query = query.Where(reservation =>
                    reservation.Customer != null &&
                    (
                        reservation.Customer.Name.Contains(Search) ||
                        (
                            reservation.Customer.Phone != null &&
                            reservation.Customer.Phone.Contains(Search)
                        )
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

            Reservations = await query
                .OrderBy(reservation => reservation.Date)
                .ThenBy(reservation => reservation.Time)
                .ThenBy(reservation => reservation.ReservationID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
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