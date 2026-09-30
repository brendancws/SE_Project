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

        public async Task OnGetAsync()
        {
            Payments = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }
    }
}