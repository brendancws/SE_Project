using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Staff
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
            var existingStaff = await _db.Staff
                .FirstOrDefaultAsync(staff =>
                    staff.StaffID == Input.StaffID);

            if (existingStaff == null)
            {
                return RedirectToPage("Index");
            }

            if (!string.IsNullOrWhiteSpace(Input.Email))
            {
                string normalisedEmail = Input.Email.Trim().ToLower();

                bool emailTaken = await _db.Staff.AnyAsync(staff =>
                    staff.StaffID != Input.StaffID &&
                    staff.Email.ToLower() == normalisedEmail);

                if (emailTaken)
                {
                    ModelState.AddModelError(
                        "Input.Email",
                        "Another Staff member already uses this email.");
                }
            }

            // The password is not edited through this form.
            ModelState.Remove("Input.Password");

            if (!ModelState.IsValid)
            {
                Input.Password = string.Empty;
                return Page();
            }

            existingStaff.Name = Input.Name.Trim();
            existingStaff.Email = Input.Email.Trim();
            existingStaff.Phone = string.IsNullOrWhiteSpace(Input.Phone)
                ? null
                : Input.Phone.Trim();

            // Staff accounts must keep the Staff role so that the
            // existing StaffOnly policy and login checks continue to work.
            existingStaff.Role = "Staff";

            existingStaff.Status = string.Equals(
                Input.Status,
                "Inactive",
                StringComparison.OrdinalIgnoreCase)
                    ? "Inactive"
                    : "Active";

            await _db.SaveChangesAsync();

            TempData["StatusMessage"] =
                "The Staff account was updated successfully.";

            return RedirectToPage("Index");
        }
    }
}