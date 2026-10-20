
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Staff.Payments
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Payment Payment { get; set; } = null!;

        [BindProperty]
        public string NewStatus { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            Payment = payment;
            NewStatus = payment.Status ?? "Pending";

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            // Load payment with order and customer details
            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            Payment = payment;

            // Validate the selected payment status
            var allowedStatuses = new[]
            {
                "Pending",
                "Paid",
                "Failed",
                "Cancelled"
            };

            if (string.IsNullOrWhiteSpace(NewStatus) ||
                !allowedStatuses.Contains(NewStatus))
            {
                ModelState.AddModelError(
                    nameof(NewStatus),
                    "Please select a valid payment status.");

                return Page();
            }

            // Update payment status
            payment.Status = NewStatus;

            // Save the update to SQL Server
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Payment status updated successfully.";

            return RedirectToPage(
                "/Staff/Payments/Details",
                new { id = payment.PaymentID });
        }
    }
}
