using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Orders
{
    // Protected by the CustomerOnly folder convention in Program.cs
    public class ReceiptModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ReceiptModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class ReceiptLine
        {
            public string Name { get; set; } = string.Empty;
            public decimal UnitPrice { get; set; }
            public int Quantity { get; set; }
            public decimal Subtotal { get; set; }
        }

        public int OrderID { get; private set; }
        public DateTime? OrderDate { get; private set; }
        public string OrderStatus { get; private set; } = "Pending";
        public decimal OrderTotal { get; private set; }

        public int PaymentID { get; private set; }
        public DateTime? PaymentDate { get; private set; }
        public string PaymentMethod { get; private set; } = "-";
        public string PaymentStatus { get; private set; } = "Pending";
        public string? TransactionNo { get; private set; }
        public decimal PaymentAmount { get; private set; }

        public int ReservationID { get; private set; }
        public DateTime ReservationDate { get; private set; }
        public TimeSpan ReservationTime { get; private set; }
        public string TableNumber { get; private set; } = "-";

        public string CustomerName { get; private set; } = "Customer";

        public List<ReceiptLine> Lines { get; private set; } = new();

        public string ReceiptReference => $"PAY-{PaymentID:D6}";
        public string OrderReference => $"ORD-{OrderID:D6}";
        public string BookingReference => $"RES-{ReservationID:D6}";

        public bool IsPaid =>
            string.Equals(
                PaymentStatus,
                "Paid",
                StringComparison.OrdinalIgnoreCase);

        public bool IsPendingCash =>
            string.Equals(
                PaymentMethod,
                "Cash",
                StringComparison.OrdinalIgnoreCase) &&
            string.Equals(
                PaymentStatus,
                "Pending",
                StringComparison.OrdinalIgnoreCase);

        public async Task<IActionResult> OnGetAsync(int id)
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            // The order must belong to the logged-in customer.
            var order = await _context.CustomerOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == id &&
                    o.CustomerID == customerId.Value);

            if (order == null)
            {
                return NotFound();
            }

            // Use the most recent payment record for this order.
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(p => p.OrderID == order.OrderID)
                .OrderByDescending(p => p.PaymentID)
                .FirstOrDefaultAsync();

            // Only completed payments or pending Cash selections can produce a document.
            bool isPaid =
                string.Equals(
                    payment?.Status,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase);

            bool isPendingCash =
                string.Equals(
                    payment?.Method,
                    "Cash",
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    payment?.Status,
                    "Pending",
                    StringComparison.OrdinalIgnoreCase);

            if (payment == null || (!isPaid && !isPendingCash))
            {
                return NotFound();
            }

            var items = await _context.OrderItems
                .AsNoTracking()
                .Include(i => i.MenuItem)
                .Where(i => i.OrderID == order.OrderID)
                .ToListAsync();

            OrderID = order.OrderID;
            OrderDate = order.OrderDate;
            OrderStatus = order.Status ?? "Pending";
            OrderTotal = order.TotalAmount ?? 0m;

            PaymentID = payment.PaymentID;
            PaymentDate = payment.PaymentDate;
            PaymentMethod = payment.Method ?? "-";
            PaymentStatus = payment.Status ?? "Pending";
            TransactionNo = payment.TransactionNo;
            PaymentAmount = payment.Amount;

            CustomerName =
                order.Customer?.Name ??
                User.Identity?.Name ??
                "Customer";

            if (order.Reservation != null)
            {
                ReservationID = order.Reservation.ReservationID;
                ReservationDate = order.Reservation.Date;
                ReservationTime = order.Reservation.Time;
                TableNumber =
                    order.Reservation.Table?.TableNumber ?? "-";
            }

            Lines = items.Select(i => new ReceiptLine
            {
                Name = i.MenuItem?.Name ?? "(removed item)",
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal
            }).ToList();

            return Page();
        }

        public static string PaymentBadgeClass(string? status)
        {
            return (status ?? string.Empty).ToLowerInvariant() switch
            {
                "paid" => "bg-success",
                "pending" => "bg-warning text-dark",
                "failed" => "bg-danger",
                _ => "bg-secondary"
            };
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