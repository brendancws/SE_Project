using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.OpeningHours
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public OpeningHour Hour { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var h = await _db.OpeningHours.FindAsync(id);
            if (h == null) return RedirectToPage("Index");
            Hour = h;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _db.OpeningHours.Update(Hour);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}