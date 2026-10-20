
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

        public Customer Customer { get; set; } = null!;

        public IList<CustomerOrder> Orders { get; set; }
            = new List<CustomerOrder>();

        public IList<Reservation> Reservations { get; set; }
            = new List<Reservation>();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            // Check whether the Customer ID was provided
            if (id == null)
            {
                return NotFound();
            }

            // Retrieve the selected customer
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == id);

            if (customer == null)
            {
                return NotFound();
            }

            Customer = customer;

            // Retrieve customer reservation history
            Reservations = await _context.Reservations
                .AsNoTracking()
                .Include(r => r.Table)
                .Where(r => r.CustomerID == id)
                .OrderByDescending(r => r.Date)
                .ThenByDescending(r => r.Time)
                .ToListAsync();

            // Retrieve customer order history
            Orders = await _context.CustomerOrders
                .AsNoTracking()
                .Where(o => o.CustomerID == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Page();
        }
    }
}
