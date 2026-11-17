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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions =
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        public CreateModel(
            ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty]
        public MenuItem Item { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageFile { get; set; }

        public SelectList CategoryOptions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Item.Price = 1.00m;
            Item.Availability = true;

            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Item.Name = Item.Name?.Trim() ?? string.Empty;

            Item.Description =
                string.IsNullOrWhiteSpace(Item.Description)
                    ? null
                    : Item.Description.Trim();

            Item.ImageURL =
                string.IsNullOrWhiteSpace(Item.ImageURL)
                    ? null
                    : Item.ImageURL.Trim();

            if (string.IsNullOrWhiteSpace(Item.Name))
            {
                ModelState.AddModelError(
                    "Item.Name",
                    "Menu item name is required.");
            }

            if (Item.Price <= 0m)
            {
                ModelState.AddModelError(
                    "Item.Price",
                    "Price must be greater than RM 0.00.");
            }

            bool categoryExists = await _db.MenuCategories
                .AsNoTracking()
                .AnyAsync(category =>
                    category.CategoryID == Item.CategoryID);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "Item.CategoryID",
                    "Select a valid menu category.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            if (ImageFile != null && ImageFile.Length > 0)
            {
                string? savedPath =
                    await SaveUploadedImageAsync(ImageFile);

                if (savedPath == null)
                {
                    ModelState.AddModelError(
                        "ImageFile",
                        "Only JPG, PNG, GIF, or WEBP files up to 2 MB are allowed.");

                    await LoadCategoriesAsync();
                    return Page();
                }

                Item.ImageURL = savedPath;
            }

            _db.MenuItems.Add(Item);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        private async Task<string?> SaveUploadedImageAsync(
            IFormFile file)
        {
            string extension = Path
                .GetExtension(file.FileName)
                .ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                return null;
            }

            if (file.Length <= 0 ||
                file.Length > 2 * 1024 * 1024)
            {
                return null;
            }

            string folder = Path.Combine(
                _env.WebRootPath,
                "uploads",
                "menu");

            Directory.CreateDirectory(folder);

            string fileName =
                $"{Guid.NewGuid():N}{extension}";

            string fullPath =
                Path.Combine(folder, fileName);

            await using var stream =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew);

            await file.CopyToAsync(stream);

            return $"/uploads/menu/{fileName}";
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _db.MenuCategories
                .AsNoTracking()
                .OrderBy(category =>
                    category.CategoryName)
                .ToListAsync();

            CategoryOptions = new SelectList(
                categories,
                nameof(MenuCategory.CategoryID),
                nameof(MenuCategory.CategoryName),
                Item.CategoryID);
        }
    }
}