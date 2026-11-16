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

        public DeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public MenuItem Item { get; set; } = new();

        public bool HasOrderHistory { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var item = await _db.MenuItems
                .AsNoTracking()
                .Include(menuItem => menuItem.Category)
                .FirstOrDefaultAsync(menuItem =>
                    menuItem.MenuItemID == id);

            if (item == null)
            {
                return RedirectToPage("./Index");
            }

            Item = item;

            HasOrderHistory = await _db.OrderItems
                .AsNoTracking()
                .AnyAsync(orderItem =>
                    orderItem.MenuItemID == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var item = await _db.MenuItems
                .FirstOrDefaultAsync(menuItem =>
                    menuItem.MenuItemID == Item.MenuItemID);

            if (item == null)
            {
                return RedirectToPage("./Index");
            }

            HasOrderHistory = await _db.OrderItems
                .AsNoTracking()
                .AnyAsync(orderItem =>
                    orderItem.MenuItemID == item.MenuItemID);

            if (HasOrderHistory)
            {
                Item = await _db.MenuItems
                    .AsNoTracking()
                    .Include(menuItem => menuItem.Category)
                    .FirstAsync(menuItem =>
                        menuItem.MenuItemID == item.MenuItemID);

                ErrorMessage =
                    "This menu item cannot be deleted because it appears in existing orders. Mark it as unavailable instead.";

                return Page();
            }

            _db.MenuItems.Remove(item);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}