using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.History
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context;

        public IndexModel(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Tab { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? From { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? To { get; set; }

        [BindProperty(
            Name = "p",
            SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalCount { get; private set; }

        public int TotalPages { get; private set; } = 1;

        public string? FilterError { get; private set; }

        public string? FromText =>
            From?.ToString("yyyy-MM-dd");

        public string? ToText =>
            To?.ToString("yyyy-MM-dd");

        public class ReservationRow
        {
            public DateTime Date { get; set; }

            public TimeSpan Time { get; set; }

            public string TableNumber { get; set; }
                = "-";

            public int Guests { get; set; }

            public string Status { get; set; }
                = "Pending";
        }

        public class OrderRow
        {
            public int OrderID { get; set; }

            public DateTime? OrderDate { get; set; }

            public string BookingText { get; set; }
                = "-";

            public string Status { get; set; }
                = "Pending";

            public decimal Total { get; set; }
        }

        public class PaymentRow
        {
            public int PaymentID { get; set; }

            public int OrderID { get; set; }

            public DateTime? PaymentDate { get; set; }

            public string Method { get; set; }
                = "-";

            public string? TransactionNo { get; set; }

            public string Status { get; set; }
                = "Pending";

            public decimal Amount { get; set; }
        }

        public List<ReservationRow> Reservations
        {
            get;
            private set;
        } = new();

        public List<OrderRow> Orders
        {
            get;
            private set;
        } = new();

        public List<PaymentRow> Payments
        {
            get;
            private set;
        } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            Tab =
                (Tab ?? string.Empty)
                    .ToLowerInvariant() switch
                {
                    "orders" =>
                        "orders",

                    "payments" =>
                        "payments",

                    _ =>
                        "reservations"
                };

            if (From.HasValue &&
                To.HasValue &&
                From.Value.Date > To.Value.Date)
            {
                FilterError =
                    "The From date cannot be after the To date.";

                PageNumber = 1;

                return Page();
            }

            DateTime? fromDate =
                From?.Date;

            DateTime? toExclusive =
                To?.Date.AddDays(1);

            switch (Tab)
            {
                case "orders":
                    await LoadOrdersAsync(
                        customerId.Value,
                        fromDate,
                        toExclusive);
                    break;

                case "payments":
                    await LoadPaymentsAsync(
                        customerId.Value,
                        fromDate,
                        toExclusive);
                    break;

                default:
                    await LoadReservationsAsync(
                        customerId.Value,
                        fromDate,
                        toExclusive);
                    break;
            }

            return Page();
        }

        private async Task LoadReservationsAsync(
            int customerId,
            DateTime? fromDate,
            DateTime? toExclusive)
        {
            var query =
                _context.Reservations
                    .AsNoTracking()
                    .Where(reservation =>
                        reservation.CustomerID ==
                        customerId);

            if (fromDate.HasValue)
            {
                query =
                    query.Where(reservation =>
                        reservation.Date >=
                        fromDate.Value);
            }

            if (toExclusive.HasValue)
            {
                query =
                    query.Where(reservation =>
                        reservation.Date <
                        toExclusive.Value);
            }

            TotalCount =
                await query.CountAsync();

            SetPaging();

            Reservations =
                await query
                    .OrderByDescending(reservation =>
                        reservation.Date)
                    .ThenByDescending(reservation =>
                        reservation.Time)
                    .ThenByDescending(reservation =>
                        reservation.ReservationID)
                    .Skip(
                        (PageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .Select(reservation =>
                        new ReservationRow
                        {
                            Date =
                                reservation.Date,

                            Time =
                                reservation.Time,

                            TableNumber =
                                reservation.Table != null
                                    ? reservation.Table.TableNumber
                                    : "-",

                            Guests =
                                reservation.NumberOfGuests,

                            Status =
                                reservation.Status ??
                                "Pending"
                        })
                    .ToListAsync();
        }

        private async Task LoadOrdersAsync(
            int customerId,
            DateTime? fromDate,
            DateTime? toExclusive)
        {
            var query =
                _context.CustomerOrders
                    .AsNoTracking()
                    .Where(order =>
                        order.CustomerID ==
                        customerId);

            if (fromDate.HasValue)
            {
                query =
                    query.Where(order =>
                        order.OrderDate >=
                        fromDate.Value);
            }

            if (toExclusive.HasValue)
            {
                query =
                    query.Where(order =>
                        order.OrderDate <
                        toExclusive.Value);
            }

            TotalCount =
                await query.CountAsync();

            SetPaging();

            var orders =
                await query
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

            Orders =
                orders
                    .Select(order =>
                    {
                        string bookingText =
                            "-";

                        if (order.Reservation != null)
                        {
                            string reservationTime =
                                DateTime.Today
                                    .Add(
                                        order.Reservation.Time)
                                    .ToString("h:mm tt");

                            bookingText =
                                $"{order.Reservation.Date:dd MMM yyyy} " +
                                $"at {reservationTime} - " +
                                $"Table " +
                                $"{order.Reservation.Table?.TableNumber ?? "-"}";
                        }

                        return new OrderRow
                        {
                            OrderID =
                                order.OrderID,

                            OrderDate =
                                order.OrderDate,

                            BookingText =
                                bookingText,

                            Status =
                                order.Status ??
                                "Pending",

                            Total =
                                order.TotalAmount ??
                                0m
                        };
                    })
                    .ToList();
        }

        private async Task LoadPaymentsAsync(
            int customerId,
            DateTime? fromDate,
            DateTime? toExclusive)
        {
            /*
             * A payment belongs to a customer through its order.
             * The ownership condition prevents another customer's
             * payment records from appearing in this history.
             */
            var query =
                _context.Payments
                    .AsNoTracking()
                    .Where(payment =>
                        _context.CustomerOrders
                            .Any(order =>
                                order.OrderID ==
                                    payment.OrderID &&
                                order.CustomerID ==
                                    customerId));

            if (fromDate.HasValue)
            {
                query =
                    query.Where(payment =>
                        payment.PaymentDate >=
                        fromDate.Value);
            }

            if (toExclusive.HasValue)
            {
                query =
                    query.Where(payment =>
                        payment.PaymentDate <
                        toExclusive.Value);
            }

            TotalCount =
                await query.CountAsync();

            SetPaging();

            Payments =
                await query
                    .OrderByDescending(payment =>
                        payment.PaymentDate)
                    .ThenByDescending(payment =>
                        payment.PaymentID)
                    .Skip(
                        (PageNumber - 1) *
                        PageSize)
                    .Take(PageSize)
                    .Select(payment =>
                        new PaymentRow
                        {
                            PaymentID =
                                payment.PaymentID,

                            OrderID =
                                payment.OrderID,

                            PaymentDate =
                                payment.PaymentDate,

                            Method =
                                payment.Method ??
                                "-",

                            TransactionNo =
                                payment.TransactionNo,

                            Status =
                                payment.Status ??
                                "Pending",

                            Amount =
                                payment.Amount
                        })
                    .ToListAsync();
        }

        private void SetPaging()
        {
            TotalPages =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        TotalCount /
                        (double)PageSize));

            PageNumber =
                Math.Clamp(
                    PageNumber,
                    1,
                    TotalPages);
        }

        public static string BadgeClass(
            string? status)
        {
            return (
                status ??
                string.Empty
            ).ToLowerInvariant() switch
            {
                "pending" =>
                    "bg-warning text-dark",

                "confirmed" =>
                    "bg-success",

                "preparing" =>
                    "bg-info text-dark",

                "ready to serve" =>
                    "bg-primary",

                "served" =>
                    "bg-success",

                "completed" =>
                    "bg-dark",

                "paid" =>
                    "bg-success",

                "failed" =>
                    "bg-danger",

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