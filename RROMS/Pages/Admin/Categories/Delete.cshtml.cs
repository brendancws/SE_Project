using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Categories
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public MenuCategory Category { get; set; } = new();

        public int MenuItemCount { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var category = await _db.MenuCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.CategoryID == id);

            if (category == null)
            {
                return RedirectToPage("./Index");
            }

            Category = category;

            MenuItemCount = await _db.MenuItems
                .AsNoTracking()
                .CountAsync(menuItem =>
                    menuItem.CategoryID == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var category = await _db.MenuCategories
                .FirstOrDefaultAsync(item =>
                    item.CategoryID == Category.CategoryID);

            if (category == null)
            {
                return RedirectToPage("./Index");
            }

            MenuItemCount = await _db.MenuItems
                .AsNoTracking()
                .CountAsync(menuItem =>
                    menuItem.CategoryID == category.CategoryID);

            if (MenuItemCount > 0)
            {
                Category = category;

                ErrorMessage =
                    $"This category cannot be deleted because it contains {MenuItemCount} menu item(s). Move or delete those items first.";

                return Page();
            }

            _db.MenuCategories.Remove(category);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}