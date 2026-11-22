using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Customer.Cart
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class CheckoutModel : PageModel
    {
        // Every booking is a fixed 2-hour slot.
        private static readonly TimeSpan SlotLength =
            TimeSpan.FromHours(2);

        private readonly ApplicationDbContext _context;

        public CheckoutModel(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public class CheckoutLine
        {
            public int MenuItemID { get; set; }

            public string Name { get; set; }
                = string.Empty;

            public decimal UnitPrice { get; set; }

            public int Quantity { get; set; }

            public bool IsAvailable { get; set; }

            public decimal Subtotal =>
                UnitPrice * Quantity;
        }

        public class ReservationOption
        {
            public int ReservationID { get; set; }

            public string Text { get; set; }
                = string.Empty;
        }

        public List<CheckoutLine> Lines
        {
            get;
            private set;
        } = new();

        public List<ReservationOption> ReservationOptions
        {
            get;
            private set;
        } = new();

        public decimal Total =>
            Lines.Sum(line =>
                line.Subtotal);

        [BindProperty]
        [Required(
            ErrorMessage =
                "Please choose which reservation this order is for.")]
        [Display(Name = "Reservation")]
        public int? ReservationId { get; set; }

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
                    "Your cart is empty. Add some items before checking out.";

                return RedirectToPage(
                    "/Customer/Cart/Index");
            }

            await LoadReservationOptionsAsync(
                customerId.Value);

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
                    "Your cart is empty. Add some items before checking out.";

                return RedirectToPage(
                    "/Customer/Cart/Index");
            }

            await LoadReservationOptionsAsync(
                customerId.Value);

            // Every cart item must still be available.
            if (Lines.Any(line =>
                !line.IsAvailable))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Some items in your cart are no longer available. " +
                    "Please remove them from your cart first.");

                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            /*
             * The selected reservation must belong to the signed-in
             * customer, must not be cancelled, and must not have
             * finished its two-hour dining slot.
             */
            var reservation =
                await _context.Reservations
                    .AsNoTracking()
                    .Include(item =>
                        item.Table)
                    .FirstOrDefaultAsync(item =>
                        item.ReservationID ==
                            ReservationId &&
                        item.CustomerID ==
                            customerId.Value &&
                        item.Status !=
                            "Cancelled");

            bool reservationHasEnded =
                reservation != null &&
                reservation.Date.Date +
                reservation.Time +
                SlotLength <=
                DateTime.Now;

            if (reservation == null ||
                reservationHasEnded)
            {
                ModelState.AddModelError(
                    nameof(ReservationId),
                    "Please choose one of your current reservations.");

                return Page();
            }

            /*
             * Nothing is saved at Checkout. The selected reservation
             * is passed to Payment, where current menu prices and
             * availability are verified before the order is created.
             */
            return RedirectToPage(
                "/Customer/Payment/Index",
                new
                {
                    reservationId =
                        reservation.ReservationID
                });
        }

        private async Task LoadLinesAsync(
            int customerId)
        {
            var cart =
                CartSession.Get(
                    HttpContext.Session,
                    customerId);

            var ids =
                cart
                    .Select(line =>
                        line.MenuItemID)
                    .ToList();

            var items =
                await _context.MenuItems
                    .AsNoTracking()
                    .Where(item =>
                        ids.Contains(
                            item.MenuItemID))
                    .ToListAsync();

            var validItemIds =
                items
                    .Select(item =>
                        item.MenuItemID)
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

            Lines =
                cart
                    .Join(
                        items,
                        line =>
                            line.MenuItemID,
                        item =>
                            item.MenuItemID,
                        (line, item) =>
                            new CheckoutLine
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
                                    item.Availability ==
                                    true
                            })
                    .ToList();
        }

        private async Task LoadReservationOptionsAsync(
            int customerId)
        {
            /*
             * Load a small date window in SQL, then apply the
             * two-hour reservation rule in C#.
             */
            DateTime windowStart =
                DateTime.Today.AddDays(-1);

            var reservations =
                await _context.Reservations
                    .AsNoTracking()
                    .Include(reservation =>
                        reservation.Table)
                    .Where(reservation =>
                        reservation.CustomerID ==
                            customerId &&
                        reservation.Status !=
                            "Cancelled" &&
                        reservation.Date >=
                            windowStart)
                    .OrderBy(reservation =>
                        reservation.Date)
                    .ThenBy(reservation =>
                        reservation.Time)
                    .ToListAsync();

            ReservationOptions =
                reservations
                    .Where(reservation =>
                        reservation.Date.Date +
                        reservation.Time +
                        SlotLength >
                        DateTime.Now)
                    .Select(reservation =>
                    {
                        string reservationTime =
                            DateTime.Today
                                .Add(
                                    reservation.Time)
                                .ToString(
                                    "h:mm tt",
                                    CultureInfo.InvariantCulture);

                        return new ReservationOption
                        {
                            ReservationID =
                                reservation.ReservationID,

                            Text =
                                $"{reservation.Date:dd MMM yyyy} " +
                                $"at {reservationTime} - " +
                                $"Table " +
                                $"{reservation.Table?.TableNumber ?? "-"} " +
                                $"({reservation.Status ?? "Pending"})"
                        };
                    })
                    .ToList();
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