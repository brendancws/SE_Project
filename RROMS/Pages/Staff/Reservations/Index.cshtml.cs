using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Reservations
{
    [Authorize(Roles = "Staff")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Reservation> Reservations { get; set; }
            = new();

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ShowAll { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 10;

        public int TotalCount { get; set; }

        public int PendingCount { get; set; }

        public int ConfirmedCount { get; set; }

        public int CancelledCount { get; set; }

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalResults == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            Math.Min(
                PageNumber * PageSize,
                TotalResults);

        public DateTime PreviousDate =>
            SelectedDate!.Value.Date.AddDays(-1);

        public DateTime NextDate =>
            SelectedDate!.Value.Date.AddDays(1);

        public bool IsToday =>
            !ShowAll &&
            SelectedDate!.Value.Date == DateTime.Today;

        public async Task OnGetAsync()
        {
            DateTime date =
                (SelectedDate ?? DateTime.Today).Date;

            SelectedDate = date;

            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            var scopeQuery = _context.Reservations
                .AsNoTracking()
                .AsQueryable();

            if (!ShowAll)
            {
                DateTime nextDate = date.AddDays(1);

                scopeQuery = scopeQuery.Where(reservation =>
                    reservation.Date >= date &&
                    reservation.Date < nextDate);
            }

            TotalCount = await scopeQuery.CountAsync();

            PendingCount = await scopeQuery.CountAsync(
                reservation =>
                    reservation.Status == "Pending");

            ConfirmedCount = await scopeQuery.CountAsync(
                reservation =>
                    reservation.Status == "Confirmed");

            CancelledCount = await scopeQuery.CountAsync(
                reservation =>
                    reservation.Status == "Cancelled");

            var resultsQuery = scopeQuery
                .Include(reservation =>
                    reservation.Customer)
                .Include(reservation =>
                    reservation.Table)
                .AsQueryable();

            if (Search != null)
            {
                resultsQuery = resultsQuery.Where(reservation =>
                    reservation.Customer != null &&
                    (
                        reservation.Customer.Name.Contains(Search) ||
                        (
                            reservation.Customer.Phone != null &&
                            reservation.Customer.Phone.Contains(Search)
                        )
                    ));
            }

            TotalResults =
                await resultsQuery.CountAsync();

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalResults / (double)PageSize));

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Reservations = await resultsQuery
                .OrderByDescending(reservation =>
                    reservation.Date)
                .ThenBy(reservation =>
                    reservation.Time)
                .ThenBy(reservation =>
                    reservation.ReservationID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}