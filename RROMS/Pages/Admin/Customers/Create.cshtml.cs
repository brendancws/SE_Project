using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CreateModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public Customer Customer { get; set; } = new();

        [BindProperty]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
            ErrorMessage = "Password must be 8+ characters with uppercase, lowercase, and a number.")]
        public string? Password { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Password required on create
            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(nameof(Password), "Password is required.");
            }
            else if (!Regex.IsMatch(Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                ModelState.AddModelError(nameof(Password),
                    "Password must be 8+ characters with uppercase, lowercase, and a number.");
            }

            // Duplicate email check
            if (!string.IsNullOrWhiteSpace(Customer.Email))
            {
                bool exists = await _db.Customers
                    .AnyAsync(c => c.Email.ToLower() == Customer.Email.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Customer.Email",
                        "This email is already registered.");
                }
            }

            if (!ModelState.IsValid) return Page();

            Customer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password!);
            _db.Customers.Add(Customer);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}