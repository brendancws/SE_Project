using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Payments
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<Payment> Payments { get; set; } = new();
        public string Filter { get; set; } = "all";
        public decimal PaidTotal { get; set; }
        public decimal PendingTotal { get; set; }

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = filter;

            var query = _db.Payments.Include(p => p.Order).AsQueryable();

            if (filter == "paid")
                query = query.Where(p => p.Status == "Paid");
            else if (filter == "pending")
                query = query.Where(p => p.Status == "Pending");
            else if (filter == "refunded")
                query = query.Where(p => p.Status == "Refunded");

            Payments = await query.OrderByDescending(p => p.PaidAt).ToListAsync();

            PaidTotal = await _db.Payments
                .Where(p => p.Status == "Paid")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            PendingTotal = await _db.Payments
                .Where(p => p.Status == "Pending")
                .SumAsync(p => (decimal?)p.Amount) ?? 0;
        }
    }
}