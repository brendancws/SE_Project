using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

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
            if (!ModelState.IsValid) return Page();

            _db.RestaurantTables.Update(Table);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}