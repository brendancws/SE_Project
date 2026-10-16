
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using RROMS.Data;

namespace RROMS.Pages.Staff.Profile
{
    [Authorize(Policy = "StaffOnly")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Full Name is required.")]
            [StringLength(100)]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email Address is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Invalid phone number.")]
            public string? Phone { get; set; }

            // Display-only information
            public string? Role { get; set; }
            public string? Status { get; set; }
        }

        // Get the Staff ID from the authenticated login cookie
        private int? GetCurrentStaffId()
        {
            string? staffId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (int.TryParse(staffId, out int id) && id > 0)
            {
                return id;
            }

            return null;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int? currentStaffId = GetCurrentStaffId();

            if (currentStaffId == null)
            {
                return Challenge();
            }

            var staff = await _context.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.StaffID == currentStaffId.Value);

            if (staff == null)
            {
                return NotFound();
            }

            Input = new ProfileInputModel
            {
                Name = staff.Name,
                Email = staff.Email,
                Phone = staff.Phone,
                Role = staff.Role,
                Status = staff.Status
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? currentStaffId = GetCurrentStaffId();

            if (currentStaffId == null)
            {
                return Challenge();
            }

            // Always retrieve the staff record from the database.
            // Never trust a Staff ID supplied by a form.
            var staff = await _context.Staff
                .FirstOrDefaultAsync(
                    s => s.StaffID == currentStaffId.Value);

            if (staff == null)
            {
                return NotFound();
            }

            // Check whether the account is still active.
            if (!string.Equals(
                staff.Status,
                "Active",
                StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            // Clean up submitted information.
            Input.Name = Input.Name?.Trim() ?? string.Empty;
            Input.Email = Input.Email?.Trim() ?? string.Empty;
            Input.Phone = Input.Phone?.Trim();

            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError(
                    "Input.Name",
                    "Full Name is required.");
            }

            if (string.IsNullOrWhiteSpace(Input.Email))
            {
                ModelState.AddModelError(
                    "Input.Email",
                    "Email Address is required.");
            }

            // Don't allow two staff accounts to share one email.
            if (!string.IsNullOrWhiteSpace(Input.Email))
            {
                bool emailExists = await _context.Staff
                    .AnyAsync(s =>
                        s.StaffID != currentStaffId.Value &&
                        s.Email == Input.Email);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Input.Email",
                        "This email is already used by another staff account.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Restore trusted display-only values.
                Input.Role = staff.Role;
                Input.Status = staff.Status;

                return Page();
            }

            // Update only the editable fields.
            staff.Name = Input.Name;
            staff.Email = Input.Email;
            staff.Phone = Input.Phone;

            await _context.SaveChangesAsync();

            StatusMessage =
                "Your profile has been updated successfully.";

            return RedirectToPage();
        }
    }
}
