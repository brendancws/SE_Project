using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;

namespace RROMS.Pages.Customer.Profile
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Read-only display values (never bound from the form)
        public string Email { get; private set; } = string.Empty;
        public DateTime? DateRegistered { get; private set; }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Name is required.")]
            [StringLength(100, MinimumLength = 2,
                ErrorMessage = "Name must be between 2 and 100 characters.")]
            [Display(Name = "Full Name")]
            public string Name { get; set; } = string.Empty;

            [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
            [RegularExpression(@"^\+?[0-9\s\-]{8,20}$",
                ErrorMessage = "Enter a valid phone number (digits, spaces, dashes, optional +).")]
            [Display(Name = "Phone Number")]
            public string? Phone { get; set; }

            [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
            [Display(Name = "Address")]
            public string? Address { get; set; }

            [StringLength(100, ErrorMessage = "Dining preference cannot exceed 100 characters.")]
            [Display(Name = "Dining Preference")]
            public string? DiningPreference { get; set; }

            [StringLength(500, ErrorMessage = "Special request cannot exceed 500 characters.")]
            [Display(Name = "Special Request")]
            public string? SpecialRequest { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            Email = customer.Email;
            DateRegistered = customer.DateRegistered;

            Input = new ProfileInputModel
            {
                Name = customer.Name,
                Phone = customer.Phone,
                Address = customer.Address,
                DiningPreference = customer.DiningPreference,
                SpecialRequest = customer.SpecialRequest
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? customerId = GetCurrentCustomerId();
            if (customerId == null)
            {
                return Challenge();
            }

            // Tracked query: we are going to update this entity
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerID == customerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            // Reload read-only display values in case we redisplay the form
            Email = customer.Email;
            DateRegistered = customer.DateRegistered;

            // Whitespace-only name should fail validation
            if (string.IsNullOrWhiteSpace(Input.Name))
            {
                ModelState.AddModelError("Input.Name", "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Only editable fields are copied - Email, Password, Status untouched
            customer.Name = Input.Name.Trim();
            customer.Phone = NormaliseOptional(Input.Phone);
            customer.Address = NormaliseOptional(Input.Address);
            customer.DiningPreference = NormaliseOptional(Input.DiningPreference);
            customer.SpecialRequest = NormaliseOptional(Input.SpecialRequest);

            await _context.SaveChangesAsync();

            StatusMessage = "Your profile has been updated successfully.";

            // Post/Redirect/Get
            return RedirectToPage();
        }

        private int? GetCurrentCustomerId()
        {
            string? idValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(idValue, out int id) && id > 0 ? id : null;
        }

        private static string? NormaliseOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}