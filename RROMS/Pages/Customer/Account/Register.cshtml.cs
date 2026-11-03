using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

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
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = Email.Trim();

            if (await _context.Customers.AnyAsync(c => c.Email == email))
            {
                ModelState.AddModelError(nameof(Email), "Email already registered.");
                return Page();
            }

            var newCustomer = new RROMS.Models.Customer
            {
                Name = Name.Trim(),
                Email = email,
                Status = "Active",
                DateRegistered = DateTime.Now
            };

            newCustomer.Password = _passwordHasher.HashPassword(newCustomer, Password);

            _context.Customers.Add(newCustomer);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Login");
        }
    }
}