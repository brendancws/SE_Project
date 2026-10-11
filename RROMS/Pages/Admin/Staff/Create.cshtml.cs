using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using System.Text.RegularExpressions;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CreateModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public StaffMember Staff { get; set; } = new();

        [BindProperty]
        public string? Password { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // Debug log — will show in VS Output window during debugging
            Console.WriteLine($"=== STAFF CREATE POST ===");
            Console.WriteLine($"Name     : '{Staff.FullName}'");
            Console.WriteLine($"Username : '{Staff.Username}'");
            Console.WriteLine($"Email    : '{Staff.Email}'");
            Console.WriteLine($"Phone    : '{Staff.Phone}'");
            Console.WriteLine($"Role     : '{Staff.Role}'");
            Console.WriteLine($"Password : '{(string.IsNullOrEmpty(Password) ? "<EMPTY>" : Password)}'");

            // Password checks
            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(nameof(Password), "Password is required.");
            }
            else if (!Regex.IsMatch(Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                ModelState.AddModelError(nameof(Password),
                    "Password must be 8+ characters with uppercase, lowercase, and a number.");
            }

            // Duplicate username
            if (!string.IsNullOrWhiteSpace(Staff.Username))
            {
                bool exists = await _db.Staff
                    .AnyAsync(s => s.Username.ToLower() == Staff.Username.ToLower());
                if (exists)
                    ModelState.AddModelError("Staff.Username", "This username is already taken.");
            }

            // Duplicate email
            if (!string.IsNullOrWhiteSpace(Staff.Email))
            {
                bool exists = await _db.Staff
                    .AnyAsync(s => s.Email.ToLower() == Staff.Email.ToLower());
                if (exists)
                    ModelState.AddModelError("Staff.Email", "This email is already registered.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("--- ModelState INVALID. Errors: ---");
                foreach (var kvp in ModelState)
                {
                    foreach (var err in kvp.Value.Errors)
                    {
                        Console.WriteLine($"  [{kvp.Key}] {err.ErrorMessage}");
                    }
                }
                return Page();
            }

            Staff.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password!);
            _db.Staff.Add(Staff);
            await _db.SaveChangesAsync();

            Console.WriteLine("=== SAVE SUCCESS ===");
            return RedirectToPage("Index");
        }
    }
}