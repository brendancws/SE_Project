using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.OpeningHours
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public OpeningHour Hour { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var hour = await _db.OpeningHours
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == id);

            if (hour == null)
            {
                return RedirectToPage("./Index");
            }

            Hour = hour;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!Hour.IsClosed &&
                Hour.CloseTime <= Hour.OpenTime)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Closing time must be later than opening time.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingHour = await _db.OpeningHours
                .FirstOrDefaultAsync(item =>
                    item.Id == Hour.Id);

            if (existingHour == null)
            {
                return RedirectToPage("./Index");
            }

            existingHour.OpenTime = Hour.OpenTime;
            existingHour.CloseTime = Hour.CloseTime;
            existingHour.IsClosed = Hour.IsClosed;

            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}