using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.MenuItems
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        private static readonly HashSet<string> AllowedAvailabilityFilters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "all",
                "available",
                "unavailable"
            };

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<MenuItem> Items { get; set; } = new();

        public List<MenuCategory> Categories { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Availability { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; } = 8;

        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalResults == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            Math.Min(PageNumber * PageSize, TotalResults);

        public async Task OnGetAsync()
        {
            Search = string.IsNullOrWhiteSpace(Search)
                ? null
                : Search.Trim();

            Availability = string.IsNullOrWhiteSpace(Availability)
                ? "all"
                : Availability.Trim().ToLowerInvariant();

            if (!AllowedAvailabilityFilters.Contains(Availability))
            {
                Availability = "all";
            }

            if (PageNumber < 1)
            {
                PageNumber = 1;
            }

            Categories = await _db.MenuCategories
                .AsNoTracking()
                .OrderBy(category => category.CategoryName)
                .ToListAsync();

            if (CategoryId.HasValue &&
                !Categories.Any(category =>
                    category.CategoryID == CategoryId.Value))
            {
                CategoryId = null;
            }

            var query = _db.MenuItems
                .AsNoTracking()
                .Include(menuItem => menuItem.Category)
                .AsQueryable();

            if (Search != null)
            {
                query = query.Where(menuItem =>
                    menuItem.Name.Contains(Search) ||
                    (
                        menuItem.Description != null &&
                        menuItem.Description.Contains(Search)
                    ));
            }

            if (CategoryId.HasValue)
            {
                query = query.Where(menuItem =>
                    menuItem.CategoryID == CategoryId.Value);
            }

            query = Availability switch
            {
                "available" => query.Where(menuItem =>
                    menuItem.Availability == true),

                "unavailable" => query.Where(menuItem =>
                    menuItem.Availability != true),

                _ => query
            };

            TotalResults = await query.CountAsync();

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalResults / (double)PageSize));

            if (PageNumber > TotalPages)
            {
                PageNumber = TotalPages;
            }

            Items = await query
                .OrderBy(menuItem =>
                    menuItem.Category!.CategoryName)
                .ThenBy(menuItem => menuItem.Name)
                .ThenBy(menuItem => menuItem.MenuItemID)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
    }
}