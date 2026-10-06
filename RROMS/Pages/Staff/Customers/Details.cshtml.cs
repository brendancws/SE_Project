using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Customers
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Customer Customer { get; set; } = default!;
        public IList<CustomerOrder> Orders { get; set; } = default!;
        public IList<Reservation> Reservations { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Fetch the specific customer
            var customer = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerID == id);

            if (customer == null)
            {
                return NotFound();
            }

            Customer = customer;

            // Fetch Reservation History
            Reservations = await _context.Reservations
                .Where(r => r.CustomerID == id)
                .OrderByDescending(r => r.Date)
                .ToListAsync();

            // Fetch Order History (Using .Set<T>() to avoid plural/singular naming errors)
            Orders = await _context.Set<CustomerOrder>()
                .Where(o => o.CustomerID == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Page();
        }
    }
}