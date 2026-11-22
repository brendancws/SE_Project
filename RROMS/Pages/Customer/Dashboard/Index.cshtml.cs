using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Dashboard
{
    // Protected by the CustomerOnly folder convention in Program.cs.
    public class IndexModel : PageModel
    {
        private const int RecommendationCount = 3;

        private const int RecentOrderCount = 3;

        private readonly ApplicationDbContext _context;

        public IndexModel(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public string CustomerName { get; private set; }
            = "Customer";

        public Reservation? NextReservation { get; private set; }

        public CustomerOrder? LatestOrder { get; private set; }

        public int UnreadNotificationCount { get; private set; }

        public List<DishRecommendation> RecommendedDishes
        {
            get;
            private set;
        } = new();

        public bool HasRecommendations =>
            RecommendedDishes.Count > 0;

        public bool RecommendationsArePersonalized
        {
            get;
            private set;
        }

        public string RecommendationEyebrow
        {
            get;
            private set;
        } = "Popular Picks";

        public string RecommendationTitle
        {
            get;
            private set;
        } = "Popular Dishes For You";

        public string RecommendationDescription
        {
            get;
            private set;
        } =
            "Popular available dishes across Dineflow.";

        public string BookingReference =>
            NextReservation == null
                ? "-"
                : $"RES-{NextReservation.ReservationID:D6}";

        public string OrderReference =>
            LatestOrder == null
                ? "-"
                : $"ORD-{LatestOrder.OrderID:D6}";

        public class DishRecommendation
        {
            public int MenuItemID { get; set; }

            public string Name { get; set; }
                = string.Empty;

            public string Description { get; set; }
                = string.Empty;

            public decimal Price { get; set; }

            public string? ImageURL { get; set; }

            public string CategoryName { get; set; }
                = "Menu";

            public int PopularityQuantity { get; set; }

            public string Reason { get; set; }
                = string.Empty;
        }

        private sealed class AvailableDish
        {
            public int MenuItemID { get; set; }

            public string Name { get; set; }
                = string.Empty;

            public string Description { get; set; }
                = string.Empty;

            public decimal Price { get; set; }

            public string? ImageURL { get; set; }

            public int CategoryID { get; set; }

            public string CategoryName { get; set; }
                = "Menu";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            RROMS.Models.Customer? customer =
                await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item =>
                        item.CustomerID ==
                            customerId.Value);

            if (customer == null)
            {
                return Challenge();
            }

            CustomerName =
                string.IsNullOrWhiteSpace(
                    customer.Name)
                    ? User.Identity?.Name ??
                        "Customer"
                    : customer.Name;

            DateTime now =
                DateTime.Now;

            DateTime today =
                now.Date;

            TimeSpan currentTime =
                now.TimeOfDay;

            NextReservation =
                await _context.Reservations
                    .AsNoTracking()
                    .Include(reservation =>
                        reservation.Table)
                    .Where(reservation =>
                        reservation.CustomerID ==
                            customerId.Value &&
                        reservation.Status !=
                            "Cancelled" &&
                        (
                            reservation.Date >
                                today ||
                            (
                                reservation.Date ==
                                    today &&
                                reservation.Time >=
                                    currentTime
                            )
                        ))
                    .OrderBy(reservation =>
                        reservation.Date)
                    .ThenBy(reservation =>
                        reservation.Time)
                    .FirstOrDefaultAsync();

            LatestOrder =
                await _context.CustomerOrders
                    .AsNoTracking()
                    .Include(order =>
                        order.Reservation)
                    .ThenInclude(reservation =>
                        reservation!.Table)
                    .Where(order =>
                        order.CustomerID ==
                            customerId.Value)
                    .OrderByDescending(order =>
                        order.OrderDate)
                    .ThenByDescending(order =>
                        order.OrderID)
                    .FirstOrDefaultAsync();

            UnreadNotificationCount =
                await _context.Notifications
                    .AsNoTracking()
                    .CountAsync(notification =>
                        notification.UserID ==
                            customerId.Value &&
                        notification.UserType ==
                            "Customer" &&
                        notification.IsRead !=
                            true);

            await LoadRecommendationsAsync(
                customerId.Value);

            return Page();
        }

        private async Task LoadRecommendationsAsync(
            int customerId)
        {
            /*
             * Determine the customer's favorite category using the
             * total quantity purchased in non-cancelled orders.
             */
            var favoriteCategory =
                await _context.OrderItems
                    .AsNoTracking()
                    .Where(orderItem =>
                        orderItem.Order != null &&
                        orderItem.Order.CustomerID ==
                            customerId &&
                        orderItem.Order.Status !=
                            "Cancelled" &&
                        orderItem.MenuItem != null)
                    .GroupBy(orderItem =>
                        new
                        {
                            orderItem.MenuItem!.CategoryID,

                            CategoryName =
                                orderItem.MenuItem.Category != null
                                    ? orderItem.MenuItem.Category
                                        .CategoryName
                                    : "Menu"
                        })
                    .Select(group =>
                        new
                        {
                            CategoryID =
                                group.Key.CategoryID,

                            CategoryName =
                                group.Key.CategoryName,

                            TotalQuantity =
                                group.Sum(orderItem =>
                                    orderItem.Quantity)
                        })
                    .OrderByDescending(item =>
                        item.TotalQuantity)
                    .ThenBy(item =>
                        item.CategoryName)
                    .FirstOrDefaultAsync();

            List<AvailableDish> availableDishes =
                await _context.MenuItems
                    .AsNoTracking()
                    .Include(menuItem =>
                        menuItem.Category)
                    .Where(menuItem =>
                        menuItem.Availability ==
                            true)
                    .Select(menuItem =>
                        new AvailableDish
                        {
                            MenuItemID =
                                menuItem.MenuItemID,

                            Name =
                                menuItem.Name,

                            Description =
                                menuItem.Description ??
                                string.Empty,

                            Price =
                                menuItem.Price,

                            ImageURL =
                                menuItem.ImageURL,

                            CategoryID =
                                menuItem.CategoryID,

                            CategoryName =
                                menuItem.Category != null
                                    ? menuItem.Category
                                        .CategoryName
                                    : "Menu"
                        })
                    .ToListAsync();

            if (availableDishes.Count == 0)
            {
                RecommendedDishes =
                    new List<DishRecommendation>();

                return;
            }

            /*
             * Popularity is based on total ordered quantity across
             * non-cancelled orders. Dishes with no order history are
             * still eligible and receive a score of zero.
             */
            Dictionary<int, int> popularityByMenuItem =
                await _context.OrderItems
                    .AsNoTracking()
                    .Where(orderItem =>
                        orderItem.Order != null &&
                        orderItem.Order.Status !=
                            "Cancelled")
                    .GroupBy(orderItem =>
                        orderItem.MenuItemID)
                    .Select(group =>
                        new
                        {
                            MenuItemID =
                                group.Key,

                            TotalQuantity =
                                group.Sum(orderItem =>
                                    orderItem.Quantity)
                        })
                    .ToDictionaryAsync(
                        item =>
                            item.MenuItemID,

                        item =>
                            item.TotalQuantity);

            if (favoriteCategory != null)
            {
                List<int> recentOrderIds =
                    await _context.CustomerOrders
                        .AsNoTracking()
                        .Where(order =>
                            order.CustomerID ==
                                customerId &&
                            order.Status !=
                                "Cancelled")
                        .OrderByDescending(order =>
                            order.OrderDate)
                        .ThenByDescending(order =>
                            order.OrderID)
                        .Take(RecentOrderCount)
                        .Select(order =>
                            order.OrderID)
                        .ToListAsync();

                HashSet<int> recentlyOrderedMenuItemIds =
                    recentOrderIds.Count == 0
                        ? new HashSet<int>()
                        : (
                            await _context.OrderItems
                                .AsNoTracking()
                                .Where(orderItem =>
                                    recentOrderIds.Contains(
                                        orderItem.OrderID))
                                .Select(orderItem =>
                                    orderItem.MenuItemID)
                                .Distinct()
                                .ToListAsync()
                        ).ToHashSet();

                List<AvailableDish> favoriteCategoryDishes =
                    availableDishes
                        .Where(dish =>
                            dish.CategoryID ==
                                favoriteCategory.CategoryID)
                        .ToList();

                /*
                 * Prefer available dishes in the favorite category
                 * that were not present in the customer's latest
                 * three non-cancelled orders.
                 */
                List<AvailableDish> selectedDishes =
                    favoriteCategoryDishes
                        .Where(dish =>
                            !recentlyOrderedMenuItemIds.Contains(
                                dish.MenuItemID))
                        .OrderByDescending(dish =>
                            GetPopularity(
                                dish.MenuItemID,
                                popularityByMenuItem))
                        .ThenBy(dish =>
                            dish.Name)
                        .Take(RecommendationCount)
                        .ToList();

                /*
                 * If fewer than three unseen dishes exist, complete
                 * the row using other available dishes from the same
                 * favorite category.
                 */
                if (selectedDishes.Count <
                    RecommendationCount)
                {
                    HashSet<int> selectedIds =
                        selectedDishes
                            .Select(dish =>
                                dish.MenuItemID)
                            .ToHashSet();

                    selectedDishes.AddRange(
                        favoriteCategoryDishes
                            .Where(dish =>
                                !selectedIds.Contains(
                                    dish.MenuItemID))
                            .OrderByDescending(dish =>
                                GetPopularity(
                                    dish.MenuItemID,
                                    popularityByMenuItem))
                            .ThenBy(dish =>
                                dish.Name)
                            .Take(
                                RecommendationCount -
                                selectedDishes.Count));
                }

                if (selectedDishes.Count > 0)
                {
                    RecommendationsArePersonalized =
                        true;

                    RecommendationEyebrow =
                        "Recommended For You";

                    RecommendationTitle =
                        $"More from {favoriteCategory.CategoryName}";

                    RecommendationDescription =
                        $"Based on your previous " +
                        $"{favoriteCategory.CategoryName} orders.";

                    RecommendedDishes =
                        selectedDishes
                            .Select(dish =>
                                CreateRecommendation(
                                    dish,
                                    popularityByMenuItem,
                                    $"Because you enjoy " +
                                    $"{favoriteCategory.CategoryName}"))
                            .ToList();

                    return;
                }
            }

            /*
             * New customers, or customers whose favorite category
             * currently has no available items, receive globally
             * popular available dishes.
             */
            RecommendationsArePersonalized =
                false;

            RecommendationEyebrow =
                "Popular Picks";

            RecommendationTitle =
                "Popular Dishes For You";

            RecommendationDescription =
                "Popular available dishes across Dineflow.";

            RecommendedDishes =
                availableDishes
                    .OrderByDescending(dish =>
                        GetPopularity(
                            dish.MenuItemID,
                            popularityByMenuItem))
                    .ThenBy(dish =>
                        dish.Name)
                    .Take(RecommendationCount)
                    .Select(dish =>
                        CreateRecommendation(
                            dish,
                            popularityByMenuItem,
                            GetPopularity(
                                dish.MenuItemID,
                                popularityByMenuItem) > 0
                                ? "Popular with Dineflow customers"
                                : "Available to try today"))
                    .ToList();
        }

        private static DishRecommendation
            CreateRecommendation(
                AvailableDish dish,
                IReadOnlyDictionary<int, int>
                    popularityByMenuItem,
                string reason)
        {
            return new DishRecommendation
            {
                MenuItemID =
                    dish.MenuItemID,

                Name =
                    dish.Name,

                Description =
                    dish.Description,

                Price =
                    dish.Price,

                ImageURL =
                    dish.ImageURL,

                CategoryName =
                    dish.CategoryName,

                PopularityQuantity =
                    GetPopularity(
                        dish.MenuItemID,
                        popularityByMenuItem),

                Reason =
                    reason
            };
        }

        private static int GetPopularity(
            int menuItemId,
            IReadOnlyDictionary<int, int>
                popularityByMenuItem)
        {
            return
                popularityByMenuItem.TryGetValue(
                    menuItemId,
                    out int quantity)
                    ? quantity
                    : 0;
        }

        public static string ReservationBadgeClass(
            string? status)
        {
            return (
                status ??
                string.Empty
            ).ToLowerInvariant() switch
            {
                "confirmed" =>
                    "bg-success",

                "pending" =>
                    "bg-warning text-dark",

                "cancelled" =>
                    "bg-danger",

                _ =>
                    "bg-secondary"
            };
        }

        public static string OrderBadgeClass(
            string? status)
        {
            return (
                status ??
                string.Empty
            ).ToLowerInvariant() switch
            {
                "pending" =>
                    "bg-warning text-dark",

                "preparing" =>
                    "bg-info text-dark",

                "ready to serve" =>
                    "bg-primary",

                "served" =>
                    "bg-success",

                "completed" =>
                    "bg-dark",

                "cancelled" =>
                    "bg-danger",

                _ =>
                    "bg-secondary"
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