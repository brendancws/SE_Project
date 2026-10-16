
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
                return NotFound();

            Order = order;
            NewStatus = order.Status ?? "Pending";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // Always load the saved order from the database
            var order = await _context.CustomerOrders
                .Include(o => o.Customer)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
                return NotFound();

            Order = order;

            string currentStatus = order.Status ?? "Pending";

            // Define valid order-status transitions
            var transitions = new Dictionary<string, string[]>
            {
                ["Pending"] = new[] { "Preparing", "Cancelled" },
                ["Preparing"] = new[] { "Ready to Serve", "Cancelled" },
                ["Ready to Serve"] = new[] { "Served", "Cancelled" },
                ["Served"] = new[] { "Completed" },
                ["Completed"] = Array.Empty<string>(),
                ["Cancelled"] = Array.Empty<string>()
            };

            // Reject unknown current statuses
            if (!transitions.TryGetValue(
                currentStatus, out var allowedNextStatuses))
            {
                ModelState.AddModelError(
                    nameof(NewStatus),
                    "The current order status is invalid.");

                return Page();
            }

            // Reject skipped or reversed order stages
            if (!allowedNextStatuses.Contains(NewStatus))
            {
                ModelState.AddModelError(
                    nameof(NewStatus),
                    $"Cannot change order status from " +
                    $"{currentStatus} to {NewStatus}.");

                return Page();
            }

            // Save the new order status
            order.Status = NewStatus;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Order status updated successfully.";

            return RedirectToPage(
                "/Staff/Orders/Details",
                new { id = order.OrderID });
        }
    }
}
