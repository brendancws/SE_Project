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
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentID == id);

            if (payment == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Pending",
                "Paid",
                "Failed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(NewStatus))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid payment status.");

                Payment = payment;
                return Page();
            }

            payment.Status = NewStatus;

            await _context.SaveChangesAsync();

            return RedirectToPage(
                "/Staff/Payments/Details",
                new { id = payment.PaymentID });
        }
    }
}