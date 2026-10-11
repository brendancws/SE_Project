using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.MenuItems
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<MenuItem> Items { get; set; } = new();

        public async Task OnGetAsync()
        {
            Items = await _db.MenuItems
                .Include(m => m.Category)
                .OrderBy(m => m.Category!.Name)
                .ThenBy(m => m.Name)
                .ToListAsync();
        }
    }
}