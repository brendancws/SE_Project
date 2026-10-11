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

        [BindProperty] public StaffMember Input { get; set; } = new();

        [BindProperty] public string? Password { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            // 1. Password checks
            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(nameof(Password), "Password is required.");
            }
            else if (!Regex.IsMatch(Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                ModelState.AddModelError(nameof(Password),
                    "Password must be 8+ characters with uppercase, lowercase, and a number.");
            }

            // 2. Duplicate username
            if (!string.IsNullOrWhiteSpace(Input?.Username))
            {
                bool exists = await _db.Staff
                    .AnyAsync(s => s.Username.ToLower() == Input.Username.ToLower());
                if (exists)
                    ModelState.AddModelError("Input.Username", "This username is already taken.");
            }

            // 3. Duplicate email
            if (!string.IsNullOrWhiteSpace(Input?.Email))
            {
                bool exists = await _db.Staff
                    .AnyAsync(s => s.Email.ToLower() == Input.Email.ToLower());
                if (exists)
                    ModelState.AddModelError("Input.Email", "This email is already registered.");
            }

            if (!ModelState.IsValid) return Page();

            try
            {
                Input.PasswordHash = BCrypt.Net.BCrypt.HashPassword(Password!);
                _db.Staff.Add(Input);
                await _db.SaveChangesAsync();
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Could not save: " + ex.Message);
                return Page();
            }
        }
    }
}