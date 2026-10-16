
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace RROMS.Pages.Staff
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private readonly IPasswordHasher<RROMS.Models.Staff>
            _passwordHasher;

        public LoginModel(
            ApplicationDbContext context,
            IPasswordHasher<RROMS.Models.Staff> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public LoginInputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public class LoginInputModel
        {
            [Required(ErrorMessage = "Email is required.")]
            [EmailAddress(ErrorMessage = "Enter a valid email.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (User.Identity?.IsAuthenticated == true &&
                User.IsInRole("Staff"))
            {
                return RedirectToPage("/Staff/Dashboard/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            string? returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email.Trim();

            // Retrieve staff record by email
            var staff = await _context.Staff
                .FirstOrDefaultAsync(s => s.Email == email);

            // Use a generic message to avoid revealing
            // whether an account exists or is inactive.
            const string loginError =
                "Invalid email or password.";

            // Verify account exists, is active and has Staff role
            if (staff == null ||
                !string.Equals(
                    staff.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    staff.Role,
                    "Staff",
                    StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    string.Empty,
                    loginError);

                return Page();
            }

            // Verify securely hashed password
            PasswordVerificationResult result;

            try
            {
                result = _passwordHasher.VerifyHashedPassword(
                    staff,
                    staff.Password,
                    Input.Password);
            }
            catch (FormatException)
            {
                result = PasswordVerificationResult.Failed;
            }

            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    loginError);

                return Page();
            }

            // Upgrade older supported hash format if needed
            if (result ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                staff.Password = _passwordHasher.HashPassword(
                    staff,
                    Input.Password);

                await _context.SaveChangesAsync();
            }

            // Create authenticated Staff identity
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    staff.StaffID.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    staff.Name),

                new Claim(
                    ClaimTypes.Email,
                    staff.Email),

                new Claim(
                    ClaimTypes.Role,
                    "Staff")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            var properties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,

                AllowRefresh = true
            };

            // Issue secure authentication cookie
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                properties);

            // Only redirect to safe local Staff URLs
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl) &&
                returnUrl.StartsWith(
                    "/Staff/",
                    StringComparison.OrdinalIgnoreCase) &&
                !returnUrl.StartsWith(
                    "/Staff/Login",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToPage("/Staff/Dashboard/Index");
        }
    }
}
