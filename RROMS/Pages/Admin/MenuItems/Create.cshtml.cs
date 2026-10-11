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

        public CreateModel(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [BindProperty] public MenuItem Item { get; set; } = new();
        [BindProperty] public IFormFile? ImageFile { get; set; }
        public SelectList CategoryOptions { get; set; } = default!;

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            // Handle file upload if provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var savedPath = await SaveUploadedImageAsync(ImageFile);
                if (savedPath == null)
                {
                    ModelState.AddModelError("ImageFile", "Only .jpg, .jpeg, .png, .gif, .webp files under 2MB are allowed.");
                    await LoadCategoriesAsync();
                    return Page();
                }
                Item.ImageUrl = savedPath;
            }

            _db.MenuItems.Add(Item);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }

        private async Task<string?> SaveUploadedImageAsync(IFormFile file)
        {
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext)) return null;
            if (file.Length > 2 * 1024 * 1024) return null; // 2MB

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
            var cats = await _db.MenuCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();
            CategoryOptions = new SelectList(cats, "Id", "Name");
        }
    }
}