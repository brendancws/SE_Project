
using Microsoft.AspNetCore.Mvc;
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

        public IList<RROMS.Models.Customer> Customers { get; set; }
       = new List<RROMS.Models.Customer>();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public int TotalCustomers { get; set; }
        public int SpecialRequestsCount { get; set; }
        public int DiningPreferencesCount { get; set; }
        public int TotalReservationsCount { get; set; }

        public async Task OnGetAsync()
        {
            // Retrieve overall statistics
            TotalCustomers = await _context.Customers
                .CountAsync();

            SpecialRequestsCount = await _context.Customers
                .CountAsync(c =>
                    c.SpecialRequest != null &&
                    c.SpecialRequest != "");

            DiningPreferencesCount = await _context.Customers
                .CountAsync(c =>
                    c.DiningPreference != null &&
                    c.DiningPreference != "");

            TotalReservationsCount = await _context.Reservations
                .CountAsync();

            // Customer search
            var query = _context.Customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                string searchTerm = Search.Trim();

                query = query.Where(c =>
                    c.Name.Contains(searchTerm) ||
                    (c.Phone != null &&
                     c.Phone.Contains(searchTerm)));
            }

            // Retrieve matching customers
            Customers = await query
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
