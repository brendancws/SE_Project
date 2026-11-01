
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RROMS.Pages.Staff.Profile
{
    [Authorize(Policy = "StaffOnly")]
    public class ChangePasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RROMS.Models.Staff>
            _passwordHasher;

        public ChangePasswordModel(
            ApplicationDbContext context,
            IPasswordHasher<RROMS.Models.Staff> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public PasswordInput Input { get; set; } = new();

        public class PasswordInput
        {
            [Required(ErrorMessage = "Current password is required.")]
            [DataType(DataType.Password)]
            public string CurrentPassword { get; set; } = "";

            [Required(ErrorMessage = "New password is required.")]
            [StringLength(128, MinimumLength = 12,
                ErrorMessage = "Password must be between 12 and 128 characters.")]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = "";

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare(nameof(NewPassword),
                ErrorMessage = "Passwords do not match.")]
            [DataType(DataType.Password)]
            public string ConfirmPassword { get; set; } = "";
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            string? staffIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(staffIdValue, out int staffId))
                return Challenge();

            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.StaffID == staffId);

            if (staff == null)
                return NotFound();

            PasswordVerificationResult result;

            try
            {
                result = _passwordHasher.VerifyHashedPassword(
                    staff,
                    staff.Password,
                    Input.CurrentPassword);
            }
            catch (FormatException)
            {
                result = PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "Input.CurrentPassword",
                    "Current password is incorrect.");

                return Page();
            }

            if (Input.CurrentPassword == Input.NewPassword)
            {
                ModelState.AddModelError(
                    "Input.NewPassword",
                    "New password must be different from current password.");

                return Page();
            }

            // Securely hash the new password
            staff.Password = _passwordHasher.HashPassword(
                staff,
                Input.NewPassword);

            await _context.SaveChangesAsync();

            // Require login again after password change
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToPage("/Staff/Login");
        }
    }
}
