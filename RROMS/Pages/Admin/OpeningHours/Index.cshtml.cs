using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.OpeningHours
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<OpeningHour> Hours { get; set; } = new();

        public async Task OnGetAsync()
        {
            var existing = await _db.OpeningHours.ToListAsync();
            var missingDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()
                .Where(d => existing.All(h => h.Day != d))
                .ToList();

            if (missingDays.Any())
            {
                foreach (var d in missingDays)
                {
                    _db.OpeningHours.Add(new OpeningHour { Day = d });
                }
                await _db.SaveChangesAsync();
                existing = await _db.OpeningHours.ToListAsync();
            }

            Hours = existing.OrderBy(h => ((int)h.Day + 6) % 7).ToList();
        }
    }
}