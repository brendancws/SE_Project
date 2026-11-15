using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using AdminEntity = RROMS.Models.Admin;

namespace RROMS.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<AdminEntity> _passwordHasher;

        public LoginModel(
            ApplicationDbContext db,
            IPasswordHasher<AdminEntity> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public class LoginInput
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToPage("/Admin/Dashboard");
                }

                return RedirectToPage("/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ErrorMessage = null;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Input.Email.Trim();

            var admin = await _db.Admins
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Email.ToLower() == email.ToLower());

            if (admin == null)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            var verificationResult =
                _passwordHasher.VerifyHashedPassword(
                    admin,
                    admin.Password,
                    Input.Password);

            if (verificationResult ==
                PasswordVerificationResult.Failed)
            {
                ErrorMessage = "Invalid email or password.";
                return Page();
            }

            if (!string.Equals(
                admin.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage =
                    "This account does not have Administrator access.";

                return Page();
            }

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    admin.AdminID.ToString()),

                new(
                    ClaimTypes.Name,
                    admin.Name),

                new(
                    ClaimTypes.Email,
                    admin.Email),

                new(
                    ClaimTypes.Role,
                    "Admin")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });

            if (verificationResult ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                var trackedAdmin =
                    await _db.Admins.FindAsync(admin.AdminID);

                if (trackedAdmin != null)
                {
                    trackedAdmin.Password =
                        _passwordHasher.HashPassword(
                            trackedAdmin,
                            Input.Password);

                    await _db.SaveChangesAsync();
                }
            }

            if (!string.IsNullOrWhiteSpace(ReturnUrl) &&
                Url.IsLocalUrl(ReturnUrl))
            {
                return LocalRedirect(ReturnUrl);
            }

            return RedirectToPage("/Admin/Dashboard");
        }
    }
}