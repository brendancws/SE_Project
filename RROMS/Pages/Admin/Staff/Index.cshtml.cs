using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        public IndexModel(ApplicationDbContext db) => _db = db;

        public List<StaffMember> StaffMembers { get; set; } = new();

        public async Task OnGetAsync()
        {
            StaffMembers = await _db.Staff.OrderBy(s => s.FullName).ToListAsync();
        }
    }
}