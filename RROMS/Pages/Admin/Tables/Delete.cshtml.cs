using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Tables
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public RestaurantTable Table { get; set; } = new();

        public int ReservationCount { get; set; }

        public string? ErrorMessage { get; set; }

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

            ReservationCount = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.TableID == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(item =>
                    item.TableID == Table.TableID);

            if (table == null)
            {
                return RedirectToPage("./Index");
            }

            ReservationCount = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.TableID == table.TableID);

            if (ReservationCount > 0)
            {
                Table = table;

                ErrorMessage =
                    $"Table {table.TableNumber} cannot be deleted because it is referenced by {ReservationCount} reservation record(s). Mark the table as unavailable instead.";

                return Page();
            }

            _db.RestaurantTables.Remove(table);
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}