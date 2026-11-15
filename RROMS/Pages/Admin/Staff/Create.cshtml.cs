using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly IPasswordHasher<RROMS.Models.Staff> _passwordHasher;

        public CreateModel(
            ApplicationDbContext db,
            IPasswordHasher<RROMS.Models.Staff> passwordHasher)
        {
            _db = db;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public RROMS.Models.Staff Input { get; set; } = new()
        {
            Role = "Staff",
            Status = "Active"
        };

        [BindProperty]
        public string? Password { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Input.Role = "Staff";

            if (string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(
                    nameof(Password),
                    "Password is required.");
            }
            else if (!Regex.IsMatch(
                Password,
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                ModelState.AddModelError(
                    nameof(Password),
                    "Password must be at least 8 characters and include uppercase, lowercase and a number.");
            }

            if (!string.IsNullOrWhiteSpace(Input.Email))
            {
                string normalisedEmail = Input.Email.Trim().ToLower();

                bool emailExists = await _db.Staff
                    .AnyAsync(staff =>
                        staff.Email.ToLower() == normalisedEmail);

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Input.Email",
                        "This email is already registered.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Input.Name = Input.Name.Trim();
            Input.Email = Input.Email.Trim();
            Input.Phone = string.IsNullOrWhiteSpace(Input.Phone)
                ? null
                : Input.Phone.Trim();

            Input.Role = "Staff";
            Input.Status = string.Equals(
                Input.Status,
                "Inactive",
                StringComparison.OrdinalIgnoreCase)
                    ? "Inactive"
                    : "Active";

            Input.Password = _passwordHasher.HashPassword(
                Input,
                Password!);

            _db.Staff.Add(Input);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] =
                "The Staff account was created successfully.";

            return RedirectToPage("Index");
        }
    }
}