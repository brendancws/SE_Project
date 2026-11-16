using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Categories
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<CategoryRow> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public int TotalResults { get; set; }

        public class CategoryRow
        {
            public int CategoryID { get; set; }

            public string CategoryName { get; set; } = string.Empty;

            public string? Description { get; set; }

            public int MenuItemCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            var query = _db.MenuCategories
                .AsNoTracking()
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(category =>
                    category.CategoryName.Contains(Search) ||
                    (
                        category.Description != null &&
                        category.Description.Contains(Search)
                    ));
            }

            Categories = await query
                .OrderBy(category => category.CategoryName)
                .Select(category => new CategoryRow
                {
                    CategoryID = category.CategoryID,
                    CategoryName = category.CategoryName,
                    Description = category.Description,

                    MenuItemCount = _db.MenuItems.Count(menuItem =>
                        menuItem.CategoryID == category.CategoryID)
                })
                .ToListAsync();

            TotalResults = Categories.Count;
        }
    }
}