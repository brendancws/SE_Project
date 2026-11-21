using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Orders
{
    // Protected by the "CustomerOnly" folder convention in Program.cs.
    public class IndexModel : PageModel
    {
        private const int PageSize = 5;

        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class OrderLineView
        {
            public string Name { get; set; }
                = string.Empty;

            public int Quantity { get; set; }

            public decimal UnitPrice { get; set; }

            public decimal Subtotal { get; set; }
        }

        public class OrderView
        {
            public int OrderID { get; set; }

            public DateTime? OrderDate { get; set; }

            public string Status { get; set; }
                = "Pending";

            public decimal Total { get; set; }

            public string BookingText { get; set; }
                = "-";

            public string? PaymentStatus { get; set; }

            public List<OrderLineView> Lines { get; set; }
                = new();
        }

        public List<OrderView> Orders { get; private set; }
            = new();

        [BindProperty(Name = "p", SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalOrders { get; private set; }

        public int ActiveOrders { get; private set; }

        public int CompletedOrders { get; private set; }

        public int PaidOrders { get; private set; }

        public int TotalPages { get; private set; } = 1;

        public bool HasPreviousPage =>
            PageNumber > 1;

        public bool HasNextPage =>
            PageNumber < TotalPages;

        public int FirstResultNumber =>
            TotalOrders == 0
                ? 0
                : ((PageNumber - 1) * PageSize) + 1;

        public int LastResultNumber =>
            TotalOrders == 0
                ? 0
                : Math.Min(
                    PageNumber * PageSize,
                    TotalOrders);

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            await LoadOrdersAsync(customerId.Value);

            return Page();
        }

        public async Task<IActionResult> OnGetStatusesAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            // Return only orders belonging to the current customer.
            var statuses = await _context.CustomerOrders
                .AsNoTracking()
                .Where(order =>
                    order.CustomerID == customerId.Value)
                .Select(order => new
                {
                    orderId =
                        order.OrderID,

                    status =
                        order.Status ?? "Pending"
                })
                .ToListAsync();

            return new JsonResult(statuses);
        }

        private async Task LoadOrdersAsync(
            int customerId)
        {
            IQueryable<RROMS.Models.CustomerOrder> ownedOrders =
                _context.CustomerOrders
                    .AsNoTracking()
                    .Where(order =>
                        order.CustomerID == customerId);

            /*
             * The summary cards are calculated from the customer's
             * complete order dataset before pagination is applied.
             */
            TotalOrders =
                await ownedOrders.CountAsync();

            ActiveOrders =
                await ownedOrders.CountAsync(order =>
                    order.Status != "Completed" &&
                    order.Status != "Cancelled");

            CompletedOrders =
                await ownedOrders.CountAsync(order =>
                    order.Status == "Completed");

            /*
             * Paid Orders follows the existing page behavior:
             * an order is counted as Paid when its most recent
             * payment attempt has Paid status.
             */
            List<int> allOwnedOrderIds =
                await ownedOrders
                    .Select(order =>
                        order.OrderID)
                    .ToListAsync();

            if (allOwnedOrderIds.Count > 0)
            {
                var paymentSummaries =
                    await _context.Payments
                        .AsNoTracking()
                        .Where(payment =>
                            allOwnedOrderIds.Contains(
                                payment.OrderID))
                        .GroupBy(payment =>
                            payment.OrderID)
                        .Select(group => new
                        {
                            OrderID =
                                group.Key,

                            LatestPaymentID =
                                group.Max(payment =>
                                    payment.PaymentID)
                        })
                        .ToListAsync();

                List<int> latestPaymentIds =
                    paymentSummaries
                        .Select(summary =>
                            summary.LatestPaymentID)
                        .ToList();

                PaidOrders =
                    await _context.Payments
                        .AsNoTracking()
                        .CountAsync(payment =>
                            latestPaymentIds.Contains(
                                payment.PaymentID) &&
                            payment.Status == "Paid");
            }
            else
            {
                PaidOrders = 0;
            }

            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(
                    TotalOrders /
                    (double)PageSize));

            PageNumber = Math.Clamp(
                PageNumber,
                1,
                TotalPages);

            /*
             * Only the five orders displayed on the current page
             * are loaded with reservation and table information.
             */
            var pagedOrders =
                await ownedOrders
                    .Include(order =>
                        order.Reservation)
                    .ThenInclude(reservation =>
                        reservation!.Table)
                    .OrderByDescending(order =>
                        order.OrderDate)
                    .ThenByDescending(order =>
                        order.OrderID)
                    .Skip(
                        (PageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .ToListAsync();

            List<int> displayedOrderIds =
                pagedOrders
                    .Select(order =>
                        order.OrderID)
                    .ToList();

            if (displayedOrderIds.Count == 0)
            {
                Orders = new List<OrderView>();
                return;
            }

            /*
             * Load line items only for the five orders displayed
             * on this page.
             */
            var displayedItems =
                await _context.OrderItems
                    .AsNoTracking()
                    .Include(item =>
                        item.MenuItem)
                    .Where(item =>
                        displayedOrderIds.Contains(
                            item.OrderID))
                    .ToListAsync();

            /*
             * Load payment attempts only for displayed orders,
             * then select the newest attempt per order.
             */
            var displayedPayments =
                await _context.Payments
                    .AsNoTracking()
                    .Where(payment =>
                        displayedOrderIds.Contains(
                            payment.OrderID))
                    .OrderByDescending(payment =>
                        payment.PaymentID)
                    .ToListAsync();

            var latestPaymentByOrder =
                displayedPayments
                    .GroupBy(payment =>
                        payment.OrderID)
                    .ToDictionary(
                        group =>
                            group.Key,

                        group =>
                            group.First());

            Orders =
                pagedOrders
                    .Select(order =>
                        new OrderView
                        {
                            OrderID =
                                order.OrderID,

                            OrderDate =
                                order.OrderDate,

                            Status =
                                order.Status ?? "Pending",

                            Total =
                                order.TotalAmount ?? 0m,

                            BookingText =
                                order.Reservation == null
                                    ? "-"
                                    : $"{order.Reservation.Date:dd MMM yyyy} " +
                                      $"at {order.Reservation.Time:hh\\:mm} - " +
                                      $"Table " +
                                      $"{order.Reservation.Table?.TableNumber ?? "-"}",

                            PaymentStatus =
                                latestPaymentByOrder.TryGetValue(
                                    order.OrderID,
                                    out var latestPayment)
                                        ? latestPayment.Status
                                        : null,

                            Lines =
                                displayedItems
                                    .Where(item =>
                                        item.OrderID ==
                                        order.OrderID)
                                    .Select(item =>
                                        new OrderLineView
                                        {
                                            Name =
                                                item.MenuItem?.Name ??
                                                "(removed item)",

                                            Quantity =
                                                item.Quantity,

                                            UnitPrice =
                                                item.UnitPrice,

                                            Subtotal =
                                                item.Subtotal
                                        })
                                    .ToList()
                        })
                    .ToList();
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return
                int.TryParse(idValue, out int id) &&
                id > 0
                    ? id
                    : null;
        }
    }
}