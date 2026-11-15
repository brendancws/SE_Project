using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using CustomerEntity = RROMS.Models.Customer;

namespace RROMS.Pages.Admin.Customers
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<CustomerEntity> Customers { get; set; } = new();

        public async Task OnGetAsync()
        {
            Customers = await _db.Customers
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}