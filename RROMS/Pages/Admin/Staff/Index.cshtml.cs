using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Admin.Staff
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<RROMS.Models.Staff> StaffMembers { get; set; } = new();

        public async Task OnGetAsync()
        {
            StaffMembers = await _db.Staff
                .AsNoTracking()
                .OrderBy(staff => staff.Name)
                .ThenBy(staff => staff.Email)
                .ToListAsync();
        }
    }
}