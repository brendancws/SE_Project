using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public RestaurantTable Table { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var table = await _db.RestaurantTables
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.TableID == id);

            if (table == null)
            {
                return RedirectToPage("./Index");
            }

            Table = table;
            return Page();
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
                        existingTable.TableID != Table.TableID &&
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

            var existingTable = await _db.RestaurantTables
                .FirstOrDefaultAsync(item =>
                    item.TableID == Table.TableID);

            if (existingTable == null)
            {
                return RedirectToPage("./Index");
            }

            existingTable.TableNumber = Table.TableNumber;
            existingTable.Capacity = Table.Capacity;
            existingTable.Location = Table.Location;
            existingTable.Status = Table.Status;

            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}