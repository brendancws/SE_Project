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

        [BindProperty] public StaffMember Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var s = await _db.Staff.FindAsync(id);
            if (s == null) return RedirectToPage("Index");
            Input = s;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            bool userTaken = await _db.Staff.AnyAsync(s =>
                s.Id != Input.Id && s.Username.ToLower() == Input.Username.ToLower());
            if (userTaken)
                ModelState.AddModelError("Input.Username", "Another staff member already uses this username.");

            bool emailTaken = await _db.Staff.AnyAsync(s =>
                s.Id != Input.Id && s.Email.ToLower() == Input.Email.ToLower());
            if (emailTaken)
                ModelState.AddModelError("Input.Email", "Another staff member already uses this email.");

            if (!ModelState.IsValid) return Page();

            _db.Staff.Update(Input);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}