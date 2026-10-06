using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Customers
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Customer> Customers { get; set; } = default!;

        public int TotalCustomers { get; set; }
        public int SpecialRequestsCount { get; set; }
        public int DiningPreferencesCount { get; set; }
        public int TotalReservationsCount { get; set; }

        public async Task OnGetAsync()
        {
            // Fixed: Use _context.Customers (plural)
            Customers = await _context.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();

            TotalCustomers = Customers.Count;
            SpecialRequestsCount = Customers.Count(c => !string.IsNullOrEmpty(c.SpecialRequest));
            DiningPreferencesCount = Customers.Count(c => !string.IsNullOrEmpty(c.DiningPreference));

            // Fetch total reservation count safely from the Reservation table
            TotalReservationsCount = await _context.Reservations.CountAsync();
        }
    }
}