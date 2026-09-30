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

        public async Task OnGetAsync()
        {
            Orders = await _context.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
    }
}