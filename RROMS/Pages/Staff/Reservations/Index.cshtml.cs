
using Microsoft.AspNetCore.Mvc;
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

        [BindProperty(SupportsGet = true)]
        public DateTime? SelectedDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task OnGetAsync()
        {
            DateTime date = (SelectedDate ?? DateTime.Today).Date;

            SelectedDate = date;

            var query = _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r =>
                    r.Date >= date &&
                    r.Date < date.AddDays(1));

            if (!string.IsNullOrWhiteSpace(Search))
            {
                string searchTerm = Search.Trim();

                query = query.Where(r =>
                    r.Customer != null &&
                    r.Customer.Name.Contains(searchTerm));
            }

            Reservations = await query
                .OrderBy(r => r.Time)
                .ToListAsync();
        }
    }
}
