using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public RROMS.Models.Staff Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var staff = await _db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.StaffID == id);

            if (staff == null)
            {
                return RedirectToPage("Index");
            }

            Input = staff;
            Input.Password = string.Empty;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var staff = await _db.Staff
                .FirstOrDefaultAsync(item =>
                    item.StaffID == Input.StaffID);

            if (staff != null)
            {
                _db.Staff.Remove(staff);
                await _db.SaveChangesAsync();

                TempData["StatusMessage"] =
                    "The Staff account was deleted.";
            }

            return RedirectToPage("Index");
        }
    }
}