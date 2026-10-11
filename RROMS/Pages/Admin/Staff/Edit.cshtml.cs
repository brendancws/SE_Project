using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

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
            bool userTaken = await _db.Staff.AnyAsync(s =>
                s.Id != Staff.Id && s.Username.ToLower() == Staff.Username.ToLower());
            if (userTaken)
                ModelState.AddModelError("Staff.Username", "Another staff member already uses this username.");

            bool emailTaken = await _db.Staff.AnyAsync(s =>
                s.Id != Staff.Id && s.Email.ToLower() == Staff.Email.ToLower());
            if (emailTaken)
                ModelState.AddModelError("Staff.Email", "Another staff member already uses this email.");

            if (!ModelState.IsValid) return Page();

            _db.Staff.Update(Staff);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}