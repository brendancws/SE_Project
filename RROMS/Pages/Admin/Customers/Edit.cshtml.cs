using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using CustomerEntity = RROMS.Models.Customer;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public CustomerEntity Customer { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var customer = await _db.Customers.FindAsync(id);

            if (customer == null)
            {
                return RedirectToPage("Index");
            }

            Customer = customer;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existingCustomer = await _db.Customers
                .FirstOrDefaultAsync(c =>
                    c.CustomerID == Customer.CustomerID);

            if (existingCustomer == null)
            {
                return RedirectToPage("Index");
            }

            if (!string.IsNullOrWhiteSpace(Customer.Email))
            {
                bool exists = await _db.Customers.AnyAsync(c =>
                    c.CustomerID != Customer.CustomerID &&
                    c.Email.ToLower() == Customer.Email.ToLower());

                if (exists)
                {
                    ModelState.AddModelError(
                        "Customer.Email",
                        "Another customer already uses this email.");
                }
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            existingCustomer.Name = Customer.Name;
            existingCustomer.Email = Customer.Email;
            existingCustomer.Phone = Customer.Phone;
            existingCustomer.Address = Customer.Address;
            existingCustomer.Status = Customer.Status;
            existingCustomer.SpecialRequest = Customer.SpecialRequest;
            existingCustomer.DiningPreference = Customer.DiningPreference;

            await _db.SaveChangesAsync();

            return RedirectToPage("Index");
        }
    }
}