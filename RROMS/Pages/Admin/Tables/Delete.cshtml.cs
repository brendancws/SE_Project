using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DeleteModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public RestaurantTable Table { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var t = await _db.RestaurantTables.FindAsync(id);
            if (t == null) return RedirectToPage("Index");
            Table = t;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var t = await _db.RestaurantTables.FindAsync(Table.Id);
            if (t != null)
            {
                _db.RestaurantTables.Remove(t);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }
    }
}