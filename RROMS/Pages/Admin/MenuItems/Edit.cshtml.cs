using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.MenuItems
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public EditModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty] public MenuItem Item { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }
        public SelectList CategoryOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var item = await _db.MenuItems.FindAsync(id);
            if (item == null) return RedirectToPage("Index");
            Item = item;
            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var existingItem = await _db.MenuItems
                .FirstOrDefaultAsync(item =>
                    item.MenuItemID == Item.MenuItemID);

            if (existingItem == null)
            {
                return RedirectToPage("Index");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            existingItem.Name = Item.Name;
            existingItem.Description = Item.Description;
            existingItem.Price = Item.Price;
            existingItem.CategoryID = Item.CategoryID;
            existingItem.Availability = Item.Availability;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                var savedPath =
                    await SaveUploadedImageAsync(ImageFile);

                if (savedPath == null)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only .jpg, .jpeg, .png, .gif or .webp files under 2 MB are allowed.");

                    await LoadCategoriesAsync();
                    return Page();
                }

                existingItem.ImageURL = savedPath;
            }

            await _db.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        private async Task<string?> SaveUploadedImageAsync(IFormFile file)
        {
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext)) return null;
            if (file.Length > 2 * 1024 * 1024) return null;

            var folder = Path.Combine(_env.WebRootPath, "uploads", "menu");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/menu/{fileName}";
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _db.MenuCategories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();

            CategoryOptions = new SelectList(
                categories,
                nameof(MenuCategory.CategoryID),
                nameof(MenuCategory.CategoryName));
        }
    }
}