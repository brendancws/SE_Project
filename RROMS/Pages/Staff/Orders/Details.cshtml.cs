using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Orders
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CustomerOrder Order { get; set; } = null!;

        [BindProperty]
        public string NewStatus { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var order = await _context.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            Order = order;
            NewStatus = order.Status ?? "Pending";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var order = await _context.CustomerOrders
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Preparing",
                "Ready to Serve",
                "Served",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(NewStatus))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid order status.");

                return Page();
            }

            order.Status = NewStatus;

            await _context.SaveChangesAsync();

            return RedirectToPage(
                "/Staff/Orders/Details",
                new { id = order.OrderID });
        }
    }
}