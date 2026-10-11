using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.MenuItems
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public DeleteModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public MenuItem Item { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var item = await _db.MenuItems
                .Include(m => m.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null) return RedirectToPage("Index");
            Item = item;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var item = await _db.MenuItems.FindAsync(Item.Id);
            if (item != null)
            {
                _db.MenuItems.Remove(item);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage("Index");
        }
    }
}