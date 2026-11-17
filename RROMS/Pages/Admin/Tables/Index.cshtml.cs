using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<RestaurantTable> Tables { get; set; } = new();

        public int TotalTables { get; set; }

        public int AvailableTables { get; set; }

        public int UnavailableTables =>
            TotalTables - AvailableTables;

        public int TotalSeats { get; set; }

        public async Task OnGetAsync()
        {
            Tables = await _db.RestaurantTables
                .AsNoTracking()
                .OrderBy(table => table.TableNumber)
                .ToListAsync();

            TotalTables = Tables.Count;

            AvailableTables = Tables.Count(table =>
                string.Equals(
                    table.Status,
                    "Available",
                    StringComparison.OrdinalIgnoreCase));

            TotalSeats = Tables.Sum(table => table.Capacity);
        }
    }
}
