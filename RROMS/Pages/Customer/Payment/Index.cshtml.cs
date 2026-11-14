using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Pages.Customer.Cart;

namespace RROMS.Pages.Customer.Payment
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        // Every booking is a fixed 2-hour slot
        // (same rule as Checkout/Reservations)
        private static readonly TimeSpan SlotLength =
            TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class PaymentLine
        {
            public int MenuItemID { get; set; }

            public string Name { get; set; } =
                string.Empty;

            public decimal UnitPrice { get; set; }

            public int Quantity { get; set; }

            public bool IsAvailable { get; set; }

            public decimal Subtotal =>
                UnitPrice * Quantity;
        }

        [BindProperty(SupportsGet = true)]
        public int ReservationId { get; set; }

        public List<PaymentLine> Lines { get; private set; } =
            new();

        public decimal Total =>
            Lines.Sum(line => line.Subtotal);

        public bool HasUnavailable =>
            Lines.Any(line => !line.IsAvailable);

        public Reservation? SelectedReservation
        {
            get;
            private set;
        }

        [BindProperty]
        [Required(
            ErrorMessage =
                "Please choose a payment method.")]
        public string Method { get; set; } =
            string.Empty;

        [BindProperty]
        public string? CardNumber { get; set; }

        public string? ResultMessage { get; private set; }

        public bool? LastAttemptSucceeded { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            await LoadLinesAsync(
                customerId.Value);

            if (!Lines.Any())
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty. Add some items " +
                    "before checking out.";

                return RedirectToPage(
                    "/Customer/Cart/Index");
            }

            SelectedReservation =
                await LoadReservationAsync(
                    customerId.Value);

            if (SelectedReservation == null)
            {
                TempData["ErrorMessage"] =
                    "Please choose one of your current " +
                    "reservations.";

                return RedirectToPage(
                    "/Customer/Cart/Checkout");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            await LoadLinesAsync(
                customerId.Value);

            if (!Lines.Any())
            {
                TempData["ErrorMessage"] =
                    "Your cart is empty. Add some items " +
                    "before checking out.";

                return RedirectToPage(
                    "/Customer/Cart/Index");
            }

            SelectedReservation =
                await LoadReservationAsync(
                    customerId.Value);

            if (SelectedReservation == null)
            {
                TempData["ErrorMessage"] =
                    "Please choose one of your current " +
                    "reservations.";

                return RedirectToPage(
                    "/Customer/Cart/Checkout");
            }

            if (Lines.Any(line =>
                !line.IsAvailable))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Some items in your cart are no longer " +
                    "available. Please remove them from your " +
                    "cart first.");

                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            decimal amount =
                Total;

            if (string.Equals(
                Method,
                "Cash",
                StringComparison.OrdinalIgnoreCase))
            {
                await using var transaction =
                    await _context.Database
                        .BeginTransactionAsync();

                try
                {
                    int newOrderId =
                        await CreateOrderAndItemsAsync(
                            customerId.Value,
                            SelectedReservation);

                    _context.Payments.Add(
                        new RROMS.Models.Payment
                        {
                            OrderID =
                                newOrderId,

                            PaymentDate =
                                DateTime.Now,

                            Amount =
                                amount,

                            Method =
                                "Cash",

                            Status =
                                "Pending",

                            TransactionNo =
                                null
                        });

                    _context.Notifications.Add(
                        new Notification
                        {
                            UserID =
                                customerId.Value,

                            UserType =
                                "Customer",

                            Message =
                                $"Order ORD-{newOrderId:D6} " +
                                "was placed. Cash payment is " +
                                "pending at the restaurant.",

                            CreatedAt =
                                DateTime.Now,

                            IsRead =
                                false
                        });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    CartSession.Clear(
                        HttpContext.Session,
                        customerId.Value);

                    TempData["OrderMessage"] =
                        $"Order #{newOrderId}: you chose " +
                        $"to pay RM {amount:0.00} in cash " +
                        "on arrival.";

                    return RedirectToPage(
                        "/Customer/Orders/Index");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            if (string.Equals(
                Method,
                "Card",
                StringComparison.OrdinalIgnoreCase))
            {
                string cardDigits =
                    new string(
                        (CardNumber ?? string.Empty)
                            .Where(char.IsDigit)
                            .ToArray());

                if (cardDigits.Length < 4)
                {
                    ModelState.AddModelError(
                        nameof(CardNumber),
                        "Please enter a card number.");

                    return Page();
                }

                // Simulated gateway rule:
                // a card ending in 0000 always fails.
                bool simulatedSuccess =
                    !cardDigits.EndsWith(
                        "0000",
                        StringComparison.Ordinal);

                // Small delay so it feels like a real
                // terminal is processing.
                await Task.Delay(1500);

                if (!simulatedSuccess)
                {
                    // Declined payment:
                    // no order, items, payment, or
                    // notification record is created.
                    ResultMessage =
                        "Payment declined. Please check your " +
                        "card details or try a different card.";

                    LastAttemptSucceeded =
                        false;

                    return Page();
                }

                await using var transaction =
                    await _context.Database
                        .BeginTransactionAsync();

                try
                {
                    int newOrderId =
                        await CreateOrderAndItemsAsync(
                            customerId.Value,
                            SelectedReservation);

                    _context.Payments.Add(
                        new RROMS.Models.Payment
                        {
                            OrderID =
                                newOrderId,

                            PaymentDate =
                                DateTime.Now,

                            Amount =
                                amount,

                            Method =
                                "Card",

                            Status =
                                "Paid",

                            TransactionNo =
                                $"SIM-{DateTime.Now:yyyyMMddHHmmss}"
                        });

                    _context.Notifications.Add(
                        new Notification
                        {
                            UserID =
                                customerId.Value,

                            UserType =
                                "Customer",

                            Message =
                                $"Order ORD-{newOrderId:D6} " +
                                "was placed and payment was " +
                                "successful.",

                            CreatedAt =
                                DateTime.Now,

                            IsRead =
                                false
                        });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    CartSession.Clear(
                        HttpContext.Session,
                        customerId.Value);

                    TempData["OrderMessage"] =
                        $"Order #{newOrderId}: payment of " +
                        $"RM {amount:0.00} was successful.";

                    return RedirectToPage(
                        "/Customer/Orders/Index");
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }

            ModelState.AddModelError(
                nameof(Method),
                "Please choose a valid payment method.");

            return Page();
        }

        // Called only after Cash is selected or Card succeeds.
        // The surrounding payment method owns the transaction.
        private async Task<int> CreateOrderAndItemsAsync(
            int customerId,
            Reservation reservation)
        {
            var order =
                new CustomerOrder
                {
                    CustomerID =
                        customerId,

                    ReservationID =
                        reservation.ReservationID,

                    OrderDate =
                        DateTime.Now,

                    Status =
                        "Pending",

                    TotalAmount =
                        Total
                };

            _context.CustomerOrders.Add(order);

            // Generates OrderID while still inside
            // the caller's database transaction.
            await _context.SaveChangesAsync();

            foreach (var line in Lines)
            {
                _context.OrderItems.Add(
                    new OrderItem
                    {
                        OrderID =
                            order.OrderID,

                        MenuItemID =
                            line.MenuItemID,

                        Quantity =
                            line.Quantity,

                        UnitPrice =
                            line.UnitPrice,

                        Subtotal =
                            line.Subtotal
                    });
            }

            return order.OrderID;
        }

        private async Task LoadLinesAsync(
            int customerId)
        {
            var cart =
                CartSession.Get(
                    HttpContext.Session,
                    customerId);

            var ids = cart
                .Select(line => line.MenuItemID)
                .ToList();

            var items = await _context.MenuItems
                .AsNoTracking()
                .Where(item =>
                    ids.Contains(item.MenuItemID))
                .ToListAsync();

            var validItemIds = items
                .Select(item => item.MenuItemID)
                .ToHashSet();

            bool removedStaleItems =
                cart.RemoveAll(line =>
                    !validItemIds.Contains(
                        line.MenuItemID)) > 0;

            if (removedStaleItems)
            {
                CartSession.Save(
                    HttpContext.Session,
                    customerId,
                    cart);
            }

            Lines = cart
                .Join(
                    items,
                    line => line.MenuItemID,
                    item => item.MenuItemID,
                    (line, item) =>
                        new PaymentLine
                        {
                            MenuItemID =
                                item.MenuItemID,

                            Name =
                                item.Name,

                            UnitPrice =
                                item.Price,

                            Quantity =
                                line.Quantity,

                            IsAvailable =
                                item.Availability == true
                        })
                .ToList();
        }

        // The reservation must belong to this customer,
        // not be cancelled, and not have ended.
        private async Task<Reservation?>
            LoadReservationAsync(
                int customerId)
        {
            var reservation =
                await _context.Reservations
                    .Include(reservation =>
                        reservation.Table)
                    .FirstOrDefaultAsync(reservation =>
                        reservation.ReservationID ==
                            ReservationId &&
                        reservation.CustomerID ==
                            customerId &&
                        reservation.Status !=
                            "Cancelled");

            if (reservation == null ||
                reservation.Date.Date +
                    reservation.Time +
                    SlotLength <= DateTime.Now)
            {
                return null;
            }

            return reservation;
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.TryParse(
                idValue,
                out int id) &&
                id > 0
                    ? id
                    : null;
        }
    }
}