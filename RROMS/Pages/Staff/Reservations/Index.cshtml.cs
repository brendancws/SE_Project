using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Reservations
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Reservation> Reservations { get; set; } = new();

        public async Task OnGetAsync()
        {
            Reservations = await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .OrderBy(r => r.Time)
                .ToListAsync();
        }
    }
}