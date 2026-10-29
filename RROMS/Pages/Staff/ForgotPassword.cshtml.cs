
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace RROMS.Pages.Staff
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
