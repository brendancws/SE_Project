using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Notifications
{
    // Protected by the CustomerOnly folder convention in Program.cs.
    public class IndexModel : PageModel
    {
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Notification> Notifications { get; private set; }
            = new();

        [BindProperty(Name = "filter", SupportsGet = true)]
        public string Filter { get; set; }
            = "all";

        [BindProperty(Name = "p", SupportsGet = true)]
        public int PageNumber { get; set; }
            = 1;

        public int TotalNotifications { get; private set; }

        public int UnreadNotifications { get; private set; }

        public int ReadNotifications { get; private set; }

        public int FilteredCount { get; private set; }

        public int TotalPages { get; private set; }
            = 1;

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public int FirstResultNumber =>
            FilteredCount == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            FilteredCount == 0
                ? 0
                : Math.Min(
                    PageNumber * PageSize,
                    FilteredCount);

        public string FilterTitle =>
            Filter switch
            {
                "unread" => "Unread Notifications",
                "read" => "Read Notifications",
                _ => "All Notifications"
            };

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            NormaliseFilter();

            await LoadNotificationsAsync(
                customerId.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostMarkReadAsync(
            int id,
            string? filter = "all",
            int p = 1)
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            Notification? notification =
                await CustomerNotifications(
                        customerId.Value)
                    .FirstOrDefaultAsync(item =>
                        item.NotificationID == id);

            if (notification == null)
            {
                return NotFound();
            }

            if (notification.IsRead != true)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();

                StatusMessage =
                    "The notification was marked as read.";
            }
            else
            {
                StatusMessage =
                    "The notification was already read.";
            }

            string normalisedFilter =
                NormaliseFilterValue(filter);

            /*
             * When marking a notification from the Unread view,
             * the record leaves that filtered result set. Returning
             * to the same page is attempted first; GET page clamping
             * safely handles an empty final page.
             */
            return RedirectToPage(
                new
                {
                    filter = normalisedFilter,
                    p = Math.Max(1, p)
                });
        }

        public async Task<IActionResult>
            OnPostMarkAllReadAsync(
                string? filter = "all")
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            List<Notification> unreadNotifications =
                await CustomerNotifications(
                        customerId.Value)
                    .Where(notification =>
                        notification.IsRead != true)
                    .ToListAsync();

            if (unreadNotifications.Count > 0)
            {
                foreach (Notification notification
                    in unreadNotifications)
                {
                    notification.IsRead = true;
                }

                await _context.SaveChangesAsync();
            }

            StatusMessage =
                unreadNotifications.Count == 0
                    ? "There were no unread notifications."
                    : $"Marked {unreadNotifications.Count} " +
                      "notification(s) as read.";

            string normalisedFilter =
                NormaliseFilterValue(filter);

            /*
             * The Unread result set becomes empty after this action,
             * so return to All. Other filters remain selected.
             */
            string destinationFilter =
                normalisedFilter == "unread"
                    ? "all"
                    : normalisedFilter;

            return RedirectToPage(
                new
                {
                    filter = destinationFilter,
                    p = 1
                });
        }

        public async Task<IActionResult>
            OnGetUnreadCountAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            int unreadCount =
                await CustomerNotifications(
                        customerId.Value)
                    .AsNoTracking()
                    .CountAsync(notification =>
                        notification.IsRead != true);

            return new JsonResult(
                new
                {
                    unreadCount
                });
        }

        private async Task LoadNotificationsAsync(
            int customerId)
        {
            IQueryable<Notification> ownedNotifications =
                CustomerNotifications(customerId)
                    .AsNoTracking();

            TotalNotifications =
                await ownedNotifications.CountAsync();

            UnreadNotifications =
                await ownedNotifications.CountAsync(
                    notification =>
                        notification.IsRead != true);

            ReadNotifications =
                TotalNotifications -
                UnreadNotifications;

            IQueryable<Notification> filteredQuery =
                Filter switch
                {
                    "unread" =>
                        ownedNotifications.Where(
                            notification =>
                                notification.IsRead != true),

                    "read" =>
                        ownedNotifications.Where(
                            notification =>
                                notification.IsRead == true),

                    _ =>
                        ownedNotifications
                };

            FilteredCount =
                await filteredQuery.CountAsync();

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    FilteredCount /
                    (double)PageSize));

            PageNumber = Math.Clamp(
                PageNumber,
                1,
                TotalPages);

            Notifications =
                await filteredQuery
                    .OrderByDescending(notification =>
                        notification.CreatedAt)
                    .ThenByDescending(notification =>
                        notification.NotificationID)
                    .Skip(
                        (PageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .ToListAsync();
        }

        private IQueryable<Notification>
            CustomerNotifications(
                int customerId)
        {
            return _context.Notifications
                .Where(notification =>
                    notification.UserID ==
                        customerId &&
                    notification.UserType ==
                        "Customer");
        }

        private void NormaliseFilter()
        {
            Filter = NormaliseFilterValue(
                Filter);
        }

        private static string NormaliseFilterValue(
            string? filter)
        {
            return (
                filter ??
                string.Empty
            ).Trim().ToLowerInvariant() switch
            {
                "unread" => "unread",
                "read" => "read",
                _ => "all"
            };
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return
                int.TryParse(
                    idValue,
                    out int id) &&
                id > 0
                    ? id
                    : null;
        }
    }
}