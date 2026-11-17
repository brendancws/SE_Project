using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Staff.Customers
{
    [Authorize(Roles = "Staff")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<RROMS.Models.Customer> Customers { get; set; }
            = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public int TotalCustomers { get; set; }

        public int SpecialRequestsCount { get; set; }

        public int DiningPreferencesCount { get; set; }

        public int TotalReservationsCount { get; set; }

        public async Task OnGetAsync()
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            TotalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync();

            SpecialRequestsCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(customer =>
                    customer.SpecialRequest != null &&
                    customer.SpecialRequest != "");

            DiningPreferencesCount = await _context.Customers
                .AsNoTracking()
                .CountAsync(customer =>
                    customer.DiningPreference != null &&
                    customer.DiningPreference != "");

            TotalReservationsCount = await _context.Reservations
                .AsNoTracking()
                .CountAsync();

            var query = _context.Customers
                .AsNoTracking()
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(customer =>
                    customer.Name.Contains(Search) ||
                    customer.Email.Contains(Search) ||
                    (
                        customer.Phone != null &&
                        customer.Phone.Contains(Search)
                    ));
            }

            Customers = await query
                .OrderBy(customer => customer.Name)
                .ThenBy(customer => customer.CustomerID)
                .ToListAsync();
        }
    }
}