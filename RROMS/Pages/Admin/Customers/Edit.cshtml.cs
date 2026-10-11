using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public Customer Customer { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c == null) return RedirectToPage("Index");
            Customer = c;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Duplicate email check (excluding this customer)
            if (!string.IsNullOrWhiteSpace(Customer.Email))
            {
                bool exists = await _db.Customers
                    .AnyAsync(c => c.Id != Customer.Id &&
                                   c.Email.ToLower() == Customer.Email.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Customer.Email",
                        "Another customer already uses this email.");
                }
            }

            if (!ModelState.IsValid) return Page();

            _db.Customers.Update(Customer);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}