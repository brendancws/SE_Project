using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DeleteModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public StaffMember Staff { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var s = await _db.Staff.FindAsync(id);
            if (s == null) return RedirectToPage("Index");
            Staff = s;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var s = await _db.Staff.FindAsync(Staff.Id);
            if (s != null)
            {
                _db.Staff.Remove(s);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }
    }
}