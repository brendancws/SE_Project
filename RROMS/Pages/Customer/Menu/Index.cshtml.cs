using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

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

        public async Task OnGetAsync()
        {
            MenuItems = await _context.MenuItems
                .AsNoTracking()
                .Include(item => item.Category)
                .OrderBy(item => item.CategoryID)
                .ThenBy(item => item.Name)
                .ToListAsync();
        }
    }
}