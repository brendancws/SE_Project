
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

            // Apply the validated next order status.
            order.Status = NewStatus;

            string notificationMessage =
                NewStatus.ToLowerInvariant() switch
                {
                    "preparing" =>
                        $"Order ORD-{order.OrderID:D6} is now Preparing.",

                    "ready to serve" =>
                        $"Order ORD-{order.OrderID:D6} is Ready to Serve.",

                    "served" =>
                        $"Order ORD-{order.OrderID:D6} has been Served.",

                    "completed" =>
                        $"Order ORD-{order.OrderID:D6} is Completed.",

                    "cancelled" =>
                        $"Order ORD-{order.OrderID:D6} has been Cancelled.",

                    _ =>
                        $"Order ORD-{order.OrderID:D6} is now {NewStatus}."
                };

            _context.Notifications.Add(
                new Notification
                {
                    UserID = order.CustomerID,
                    UserType = "Customer",
                    Message = notificationMessage,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });

            // Save the order status and notification together.
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Order status updated successfully.";

            return RedirectToPage(
                "/Staff/Orders/Details",
                new { id = order.OrderID });
        }
    }
}
