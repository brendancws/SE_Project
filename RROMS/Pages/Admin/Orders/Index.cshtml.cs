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

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<CustomerOrder> Orders { get; set; } = new();

        public string Filter { get; set; } = "all";

        public decimal TotalRevenue { get; set; }

        public int PendingCount { get; set; }

        public async Task OnGetAsync(string filter = "all")
        {
            Filter = string.IsNullOrWhiteSpace(filter)
                ? "all"
                : filter.ToLowerInvariant();

            var query = _db.CustomerOrders
                .AsNoTracking()
                .Include(order => order.Customer)
                .AsQueryable();

            if (Filter == "today")
            {
                query = query.Where(order =>
                    order.OrderDate.HasValue &&
                    order.OrderDate.Value.Date == DateTime.Today);
            }
            else if (Filter == "pending")
            {
                query = query.Where(order =>
                    order.Status == "Pending");
            }
            else if (Filter == "preparing")
            {
                query = query.Where(order =>
                    order.Status == "Preparing");
            }
            else if (Filter == "ready")
            {
                query = query.Where(order =>
                    order.Status == "Ready");
            }

            Orders = await query
                .OrderByDescending(order => order.OrderDate)
                .ThenByDescending(order => order.OrderID)
                .ToListAsync();

            TotalRevenue = await _db.CustomerOrders
                .AsNoTracking()
                .Where(order => order.Status == "Served")
                .SumAsync(order => order.TotalAmount ?? 0m);

            PendingCount = await _db.CustomerOrders
                .AsNoTracking()
                .CountAsync(order => order.Status == "Pending");
        }
    }
}