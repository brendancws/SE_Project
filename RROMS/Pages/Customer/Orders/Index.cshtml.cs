using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Orders
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class OrderLineView
        {
            public string Name { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal Subtotal { get; set; }
        }

        public class OrderView
        {
            public int OrderID { get; set; }
            public DateTime? OrderDate { get; set; }
            public string Status { get; set; } = "Pending";
            public decimal Total { get; set; }
            public string BookingText { get; set; } = "-";
            public string? PaymentStatus { get; set; }
            public List<OrderLineView> Lines { get; set; } = new();
        }

        public List<OrderView> Orders { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            // Only this customer's own orders
            var orders = await _context.CustomerOrders
                .AsNoTracking()
                .Include(o => o.Reservation)
                    .ThenInclude(r => r!.Table)
                .Where(o => o.CustomerID == customerId.Value)
                .OrderByDescending(o => o.OrderID)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderID).ToList();

            var items = await _context.OrderItems
                .AsNoTracking()
                .Include(i => i.MenuItem)
                .Where(i => orderIds.Contains(i.OrderID))
                .ToListAsync();

            // Each order's most recent payment attempt, if any
            var latestPayments = await _context.Payments
                .AsNoTracking()
                .Where(p => orderIds.Contains(p.OrderID))
                .OrderByDescending(p => p.PaymentID)
                .ToListAsync();

            Orders = orders.Select(o => new OrderView
            {
                OrderID = o.OrderID,
                OrderDate = o.OrderDate,
                Status = o.Status ?? "Pending",
                Total = o.TotalAmount ?? 0m,
                BookingText = o.Reservation == null
        ? "-"
        : $"{o.Reservation.Date:dd MMM yyyy} at {o.Reservation.Time:hh\\:mm} - Table {o.Reservation.Table?.TableNumber ?? "-"}",
                PaymentStatus = latestPayments.FirstOrDefault(p => p.OrderID == o.OrderID)?.Status,
                Lines = items
        .Where(i => i.OrderID == o.OrderID)
        .Select(i => new OrderLineView
        {
            Name = i.MenuItem?.Name ?? "(removed item)",
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.Subtotal
        })
        .ToList()
            }).ToList();

            return Page();
        }

        public async Task<IActionResult> OnGetStatusesAsync()
        {
            int? customerId = GetCurrentCustomerId();

            if (customerId == null)
            {
                return new UnauthorizedResult();
            }

            // Only return statuses belonging to the current customer.
            var statuses = await _context.CustomerOrders
                .AsNoTracking()
                .Where(o => o.CustomerID == customerId.Value)
                .Select(o => new
                {
                    orderId = o.OrderID,
                    status = o.Status ?? "Pending"
                })
                .ToListAsync();

            return new JsonResult(statuses);
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }
    }
}