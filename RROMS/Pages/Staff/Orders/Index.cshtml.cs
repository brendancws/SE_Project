
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Orders
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<CustomerOrder> Orders { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .AsQueryable();

            // Search orders by customer name
            if (!string.IsNullOrWhiteSpace(Search))
            {
                string searchTerm = Search.Trim();

                query = query.Where(o =>
                    o.Customer != null &&
                    o.Customer.Name.Contains(searchTerm));
            }

            // Filter orders by status
            if (!string.IsNullOrWhiteSpace(StatusFilter)
                && StatusFilter != "All")
            {
                var allowedStatuses = new[]
                {
                    "Pending",
                    "Preparing",
                    "Ready to Serve",
                    "Served",
                    "Completed",
                    "Cancelled"
                };

                if (allowedStatuses.Contains(StatusFilter))
                {
                    query = query.Where(o =>
                        o.Status == StatusFilter);
                }
            }

            // Display newest orders first
            Orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}
