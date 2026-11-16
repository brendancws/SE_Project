using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Categories
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CreateModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public MenuCategory Category { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            Category.CategoryName =
                Category.CategoryName?.Trim() ?? string.Empty;

            Category.Description =
                string.IsNullOrWhiteSpace(Category.Description)
                    ? null
                    : Category.Description.Trim();

            if (string.IsNullOrWhiteSpace(Category.CategoryName))
            {
                ModelState.AddModelError(
                    "Category.CategoryName",
                    "Category name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _db.MenuCategories.Add(Category);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}