using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Categories
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public EditModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public MenuCategory Category { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var cat = await _db.MenuCategories.FindAsync(id);
            if (cat == null) return RedirectToPage("Index");
            Category = cat;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _db.MenuCategories.Update(Category);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}