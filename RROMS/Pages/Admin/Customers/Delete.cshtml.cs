using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using CustomerEntity = RROMS.Models.Customer;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public DeleteModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public CustomerEntity Customer { get; set; } = new();

        public int ReservationCount { get; set; }

        public int OrderCount { get; set; }

        public int NotificationCount { get; set; }

        public int FeedbackCount { get; set; }

        public int PaymentCount { get; set; }

        public bool HasHistory =>
            ReservationCount > 0 ||
            OrderCount > 0 ||
            NotificationCount > 0 ||
            FeedbackCount > 0 ||
            PaymentCount > 0;

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.CustomerID == id);

            if (customer == null)
            {
                return RedirectToPage("./Index");
            }

            Customer = customer;

            await LoadDependencyCountsAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(item =>
                    item.CustomerID ==
                    Customer.CustomerID);

            if (customer == null)
            {
                return RedirectToPage("./Index");
            }

            await LoadDependencyCountsAsync(
                customer.CustomerID);

            if (HasHistory)
            {
                Customer = customer;

                ErrorMessage =
                    "This customer cannot be deleted because the account has restaurant activity history. Set the account to Inactive instead.";

                return Page();
            }

            _db.Customers.Remove(customer);
            await _db.SaveChangesAsync();

            TempData["StatusMessage"] =
                "The customer account was deleted.";

            return RedirectToPage("./Index");
        }

        private async Task LoadDependencyCountsAsync(
            int customerId)
        {
            ReservationCount = await _db.Reservations
                .AsNoTracking()
                .CountAsync(reservation =>
                    reservation.CustomerID == customerId);

            OrderCount = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order =>
                    order.CustomerID == customerId);

            NotificationCount = await _db.Notifications
                .AsNoTracking()
                .CountAsync(notification =>
                    notification.UserID == customerId &&
                    notification.UserType == "Customer");

            FeedbackCount = await _db.Feedbacks
                .AsNoTracking()
                .CountAsync(feedback =>
                    feedback.Reservation != null &&
                    feedback.Reservation.CustomerID ==
                    customerId);

            PaymentCount = await _db.Payments
                .AsNoTracking()
                .CountAsync(payment =>
                    payment.Order != null &&
                    payment.Order.CustomerID ==
                    customerId);
        }
    }
}