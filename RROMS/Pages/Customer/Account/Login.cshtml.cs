using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;
using System.Security.Claims;

namespace RROMS.Pages.Customer.Account
{
    public class LoginModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RROMS.Models.Customer> _passwordHasher;

        public LoginModel(ApplicationDbContext context, IPasswordHasher<RROMS.Models.Customer> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }

        // Bound from the query string, e.g. ?ReturnUrl=%2FCustomer%2FProfile
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var customer = _context.Customers.FirstOrDefault(c => c.Email == Email);
            if (customer == null)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return Page();
            }

            var result = _passwordHasher.VerifyHashedPassword(customer, customer.Password, Password);

            if (result == PasswordVerificationResult.Success)
            {
                // Create the cookie claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, customer.CustomerID.ToString()),
                    new Claim(ClaimTypes.Name, customer.Name),
                    new Claim(ClaimTypes.Email, customer.Email),
                    new Claim(ClaimTypes.Role, "Customer") // THIS IS CRITICAL
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                // Only redirect to ReturnUrl if it's a safe, local path.
                // This stops someone crafting a link like
                // /Customer/Account/Login?ReturnUrl=https://evil.com
                if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
                {
                    return LocalRedirect(ReturnUrl);
                }

                return RedirectToPage("/Customer/Dashboard/Index");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return Page();
        }
    }
}