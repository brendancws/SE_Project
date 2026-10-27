using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Account
{
    public class RegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<RROMS.Models.Customer> _passwordHasher;

        public RegisterModel(ApplicationDbContext context, IPasswordHasher<RROMS.Models.Customer> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public string Name { get; set; }
        [BindProperty]
        public string Email { get; set; }
        [BindProperty]
        public string Password { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (_context.Customers.Any(c => c.Email == Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return Page();
            }

            var newCustomer = new RROMS.Models.Customer
            {
                Name = Name,
                Email = Email,
                Status = "Active",
                DateRegistered = DateTime.Now
            };

            // Hash the password using the injected service
            newCustomer.Password = _passwordHasher.HashPassword(newCustomer, Password);

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Login");
        }
    }
}