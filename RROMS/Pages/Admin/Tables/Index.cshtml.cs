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
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<RestaurantTable> Tables { get; set; } = new();

        public async Task OnGetAsync()
        {
            Tables = await _db.RestaurantTables
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }
    }
}