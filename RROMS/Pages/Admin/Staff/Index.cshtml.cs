using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private static readonly HashSet<string> AllowedStatusFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "active",
                "inactive"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<RROMS.Models.Staff> StaffMembers { get; set; }
            = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; } = "all";

        public int TotalResults { get; set; }

        public int ActiveCount { get; set; }

        public int InactiveCount { get; set; }

        public async Task OnGetAsync()
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            Status = string.IsNullOrWhiteSpace(Status)
                ? "all"
                : Status.Trim().ToLowerInvariant();

            if (!AllowedStatusFilters.Contains(Status))
            {
                Status = "all";
            }

            ActiveCount = await _db.Staff
                .AsNoTracking()
                .CountAsync(staff =>
                    staff.Status == "Active");

            InactiveCount = await _db.Staff
                .AsNoTracking()
                .CountAsync(staff =>
                    staff.Status != "Active");

            var query = _db.Staff
                .AsNoTracking()
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(staff =>
                    staff.Name.Contains(Search) ||
                    staff.Email.Contains(Search) ||
                    (
                        staff.Phone != null &&
                        staff.Phone.Contains(Search)
                    ));
            }

            query = Status switch
            {
                "active" => query.Where(staff =>
                    staff.Status == "Active"),

                "inactive" => query.Where(staff =>
                    staff.Status != "Active"),

                _ => query
            };

            StaffMembers = await query
                .OrderBy(staff => staff.Name)
                .ThenBy(staff => staff.Email)
                .ThenBy(staff => staff.StaffID)
                .ToListAsync();

            TotalResults = StaffMembers.Count;
        }
    }
}