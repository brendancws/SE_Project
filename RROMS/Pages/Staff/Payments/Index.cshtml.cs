
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Payments
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Payment> Payments { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .AsQueryable();

            // Search by customer name
            if (!string.IsNullOrWhiteSpace(Search))
            {
                string searchTerm = Search.Trim();

                query = query.Where(p =>
                    p.Order != null &&
                    p.Order.Customer != null &&
                    p.Order.Customer.Name.Contains(searchTerm));
            }

            // Filter by payment status
            if (!string.IsNullOrWhiteSpace(StatusFilter)
                && StatusFilter != "All")
            {
                var allowedStatuses = new[]
                {
                    "Pending",
                    "Paid",
                    "Failed",
                    "Cancelled"
                };

                if (allowedStatuses.Contains(StatusFilter))
                {
                    query = query.Where(p =>
                        p.Status == StatusFilter);
                }
            }

            Payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}
