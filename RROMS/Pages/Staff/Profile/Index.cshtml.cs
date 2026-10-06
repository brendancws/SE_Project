using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Profile
{
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
        public string StatusMessage { get; set; }

        public class ProfileInputModel
        {
            public int StaffID { get; set; }

            [Required(ErrorMessage = "Full Name is required.")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email Address is required.")]
            [EmailAddress(ErrorMessage = "Invalid email format.")]
            public string Email { get; set; } = string.Empty;

            public string? Phone { get; set; }

            public string? Role { get; set; }
            public string? Status { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // TODO: Teammate - Replace this hardcoded ID with the actual login session ID once authentication is ready.
            // Example: int currentStaffId = HttpContext.Session.GetInt32("StaffID").Value;
            int currentStaffId = 1;

            var staff = await _context.Set<RROMS.Models.Staff>().FindAsync(currentStaffId);

            if (staff == null)
            {
                return NotFound();
            }

            Input = new ProfileInputModel
            {
                StaffID = staff.StaffID,
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // TODO: Teammate - Replace this hardcoded ID with the actual login session ID once authentication is ready.
            // Example: int currentStaffId = HttpContext.Session.GetInt32("StaffID").Value;
            int currentStaffId = 1;

            var staff = await _context.Set<RROMS.Models.Staff>().FindAsync(currentStaffId);

            if (staff == null)
            {
                return NotFound();
            }

            staff.Name = Input.Name;
            staff.Email = Input.Email;
            staff.Phone = Input.Phone;

            await _context.SaveChangesAsync();

            StatusMessage = "Your profile has been updated successfully.";
            return RedirectToPage();
        }
    }
}