using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Staff
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
        public RROMS.Models.Staff Input { get; set; } = new();

        public int AssignedOrderCount { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var staff = await _db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.StaffID == id);

            if (staff == null)
            {
                return RedirectToPage("./Index");
            }

            Input = staff;
            Input.Password = string.Empty;

            AssignedOrderCount = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.StaffID == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var staff = await _db.Staff
                .FirstOrDefaultAsync(item =>
                    item.StaffID == Input.StaffID);

            if (staff == null)
            {
                return RedirectToPage("./Index");
            }

            AssignedOrderCount = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.StaffID == staff.StaffID);

            if (AssignedOrderCount > 0)
            {
                Input = staff;
                Input.Password = string.Empty;

                ErrorMessage =
                    $"This Staff account cannot be deleted because it is assigned to {AssignedOrderCount} order record(s). Set the account to Inactive instead.";

                return Page();
            }

            _db.Staff.Remove(staff);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] =
                "The Staff account was deleted.";

            return RedirectToPage("./Index");
        }
    }
}