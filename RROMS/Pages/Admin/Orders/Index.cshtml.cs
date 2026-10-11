using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Orders
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<Order> Orders { get; set; } = new();
        public string Filter { get; set; } = "all";
        public decimal TotalRevenue { get; set; }
        public int PendingCount { get; set; }

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = filter;

            var query = _db.Orders.AsQueryable();

            if (filter == "today")
                query = query.Where(o => o.OrderDate.Date == DateTime.Today);
            else if (filter == "pending")
                query = query.Where(o => o.Status == "Pending");
            else if (filter == "preparing")
                query = query.Where(o => o.Status == "Preparing");
            else if (filter == "ready")
                query = query.Where(o => o.Status == "Ready");

            Orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            TotalRevenue = await _db.Orders
                .Where(o => o.Status == "Served")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            PendingCount = await _db.Orders.CountAsync(o => o.Status == "Pending");
        }
    }
}