using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Notifications
{
    // Protected by the CustomerOnly folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Notification> Notifications { get; private set; }
            = new();

        public int UnreadCount => Notifications.Count(n => n.IsRead != true);

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            Notifications = await CustomerNotifications(customerId.Value)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.NotificationID)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            Notification? notification =
                await CustomerNotifications(customerId.Value)
                    .FirstOrDefaultAsync(n => n.NotificationID == id);

            if (notification == null)
            {
                return NotFound();
            }

            if (notification.IsRead != true)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            List<Notification> unreadNotifications =
                await CustomerNotifications(customerId.Value)
                    .Where(n => n.IsRead != true)
                    .ToListAsync();

            if (unreadNotifications.Count > 0)
            {
                foreach (Notification notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            StatusMessage = unreadNotifications.Count == 0
                ? "There were no unread notifications."
                : $"Marked {unreadNotifications.Count} notification(s) as read.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetUnreadCountAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            int unreadCount =
                await CustomerNotifications(customerId.Value)
                    .CountAsync(n => n.IsRead != true);

            return new JsonResult(new
            {
                unreadCount
            });
        }

        private IQueryable<Notification> CustomerNotifications(int customerId)
        {
            return _context.Notifications.Where(n =>
                n.UserID == customerId &&
                n.UserType == "Customer");
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(idValue, out int id) && id > 0
                ? id
                : null;
        }
    }
}