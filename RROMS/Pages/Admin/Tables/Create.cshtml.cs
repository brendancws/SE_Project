using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public CreateModel(ApplicationDbContext db) => _db = db;

        [BindProperty] public RestaurantTable Table { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _db.RestaurantTables.Add(Table);
            await _db.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}