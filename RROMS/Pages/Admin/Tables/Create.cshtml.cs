using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreateModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public RestaurantTable Table { get; set; } = new();

        public void OnGet()
        {
            Table.Capacity = 2;
            Table.Status = "Available";
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Table.TableNumber =
                Table.TableNumber?.Trim() ?? string.Empty;

            Table.Location =
                string.IsNullOrWhiteSpace(Table.Location)
                    ? null
                    : Table.Location.Trim();

            if (string.IsNullOrWhiteSpace(Table.TableNumber))
            {
                ModelState.AddModelError(
                    "Table.TableNumber",
                    "Table number is required.");
            }

            if (Table.Capacity < 1 || Table.Capacity > 50)
            {
                ModelState.AddModelError(
                    "Table.Capacity",
                    "Capacity must be between 1 and 50 seats.");
            }

            bool duplicateExists =
                await _db.RestaurantTables
                    .AsNoTracking()
                    .AnyAsync(existingTable =>
                        existingTable.TableNumber.ToLower() ==
                        Table.TableNumber.ToLower());

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    "Table.TableNumber",
                    "A table with this number already exists.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _db.RestaurantTables.Add(Table);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}