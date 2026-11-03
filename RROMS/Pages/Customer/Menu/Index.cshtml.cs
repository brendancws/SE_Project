using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Pages.Customer.Cart;

namespace RROMS.Pages.Customer.Menu
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<MenuItem> MenuItems { get; private set; }
            = new List<MenuItem>();

        public int CartItemCount { get; private set; }

        public async Task OnGetAsync()
        {
            MenuItems = await _context.MenuItems
                .AsNoTracking()
                .Include(item => item.Category)
                .OrderBy(item => item.CategoryID)
                .ThenBy(item => item.Name)
                .ToListAsync();

            string? customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(customerIdValue, out int customerId) && customerId > 0)
            {
                CartItemCount = CartSession.Get(HttpContext.Session, customerId)
                    .Sum(line => line.Quantity);
            }
        }
    }
}
