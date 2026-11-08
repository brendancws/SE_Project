using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using CustomerModel = RROMS.Models.Customer;

namespace RROMS.Pages.Customer.Profile
{
    // Protected by the "CustomerOnly" folder convention in Program.cs
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private readonly IPasswordHasher<CustomerModel>
    _passwordHasher;

        public IndexModel(
            ApplicationDbContext context,
            IPasswordHasher<CustomerModel> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // Read-only display values.
        public string Email { get; private set; } =
            string.Empty;

        public DateTime? DateRegistered
        {
            get;
            private set;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } =
            new();

        [BindProperty]
        public ChangePasswordInputModel PasswordInput
        {
            get;
            set;
        } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ProfileInputModel
        {
            [Required(
                ErrorMessage = "Name is required.")]
            [StringLength(
                100,
                MinimumLength = 2,
                ErrorMessage =
                    "Name must be between 2 and 100 characters.")]
            [Display(Name = "Full Name")]
            public string Name { get; set; } =
                string.Empty;

            [StringLength(
                20,
                ErrorMessage =
                    "Phone number cannot exceed 20 characters.")]
            [RegularExpression(
                @"^\+?[0-9\s\-]{8,20}$",
                ErrorMessage =
                    "Enter a valid phone number " +
                    "(digits, spaces, dashes, optional +).")]
            [Display(Name = "Phone Number")]
            public string? Phone { get; set; }

            [StringLength(
                255,
                ErrorMessage =
                    "Address cannot exceed 255 characters.")]
            [Display(Name = "Address")]
            public string? Address { get; set; }

            [StringLength(
                100,
                ErrorMessage =
                    "Dining preference cannot exceed " +
                    "100 characters.")]
            [Display(Name = "Dining Preference")]
            public string? DiningPreference
            {
                get;
                set;
            }

            [StringLength(
                500,
                ErrorMessage =
                    "Special request cannot exceed " +
                    "500 characters.")]
            [Display(Name = "Special Request")]
            public string? SpecialRequest
            {
                get;
                set;
            }
        }

        public class ChangePasswordInputModel
        {
            [Required(
                ErrorMessage =
                    "Please enter your current password.")]
            [DataType(DataType.Password)]
            [Display(Name = "Current Password")]
            public string CurrentPassword { get; set; } =
                string.Empty;

            [Required(
                ErrorMessage =
                    "Please enter a new password.")]
            [StringLength(
                100,
                MinimumLength = 8,
                ErrorMessage =
                    "The new password must be at least " +
                    "8 characters.")]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string NewPassword { get; set; } =
                string.Empty;

            [Required(
                ErrorMessage =
                    "Please confirm your new password.")]
            [DataType(DataType.Password)]
            [Compare(
                nameof(NewPassword),
                ErrorMessage =
                    "The new password and confirmation " +
                    "password do not match.")]
            [Display(Name = "Confirm New Password")]
            public string ConfirmPassword { get; set; } =
                string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            CustomerModel? customer =
                await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(customer =>
                        customer.CustomerID ==
                            customerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            LoadDisplayValues(customer);

            Input =
                new ProfileInputModel
                {
                    Name =
                        customer.Name,

                    Phone =
                        customer.Phone,

                    Address =
                        customer.Address,

                    DiningPreference =
                        customer.DiningPreference,

                    SpecialRequest =
                        customer.SpecialRequest
                };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate only the Edit Profile form.
            ModelState.Clear();

            TryValidateModel(
                Input,
                nameof(Input));

            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            CustomerModel? customer =
                await _context.Customers
                    .FirstOrDefaultAsync(customer =>
                        customer.CustomerID ==
                            customerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            LoadDisplayValues(customer);

            if (string.IsNullOrWhiteSpace(
                Input.Name))
            {
                ModelState.AddModelError(
                    "Input.Name",
                    "Name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            customer.Name =
                Input.Name.Trim();

            customer.Phone =
                NormaliseOptional(Input.Phone);

            customer.Address =
                NormaliseOptional(Input.Address);

            customer.DiningPreference =
                NormaliseOptional(
                    Input.DiningPreference);

            customer.SpecialRequest =
                NormaliseOptional(
                    Input.SpecialRequest);

            await _context.SaveChangesAsync();

            StatusMessage =
                "Your profile has been updated successfully.";

            return RedirectToPage();
        }

        public async Task<IActionResult>
            OnPostChangePasswordAsync()
        {
            // Validate only the Change Password form.
            ModelState.Clear();

            TryValidateModel(
                PasswordInput,
                nameof(PasswordInput));

            int? customerId =
                GetCurrentCustomerId();

            if (customerId == null)
            {
                return Challenge();
            }

            CustomerModel? customer =
                await _context.Customers
                    .FirstOrDefaultAsync(customer =>
                        customer.CustomerID ==
                            customerId.Value);

            if (customer == null)
            {
                return NotFound();
            }

            LoadDisplayValues(customer);

            Input =
                new ProfileInputModel
                {
                    Name =
                        customer.Name,

                    Phone =
                        customer.Phone,

                    Address =
                        customer.Address,

                    DiningPreference =
                        customer.DiningPreference,

                    SpecialRequest =
                        customer.SpecialRequest
                };

            if (!ModelState.IsValid)
            {
                return Page();
            }

            PasswordVerificationResult result =
                _passwordHasher.VerifyHashedPassword(
                    customer,
                    customer.Password,
                    PasswordInput.CurrentPassword);

            bool currentPasswordValid =
                result ==
                    PasswordVerificationResult.Success ||
                result ==
                    PasswordVerificationResult
                        .SuccessRehashNeeded;

            if (!currentPasswordValid)
            {
                ModelState.AddModelError(
                    "PasswordInput.CurrentPassword",
                    "The current password is incorrect.");

                return Page();
            }

            if (string.Equals(
                PasswordInput.CurrentPassword,
                PasswordInput.NewPassword,
                StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    "PasswordInput.NewPassword",
                    "The new password must be different " +
                    "from the current password.");

                return Page();
            }

            customer.Password =
                _passwordHasher.HashPassword(
                    customer,
                    PasswordInput.NewPassword);

            await _context.SaveChangesAsync();

            StatusMessage =
                "Your password has been changed successfully.";

            return RedirectToPage();
        }

        private void LoadDisplayValues(
            CustomerModel customer)
        {
            Email =
                customer.Email;

            DateRegistered =
                customer.DateRegistered;
        }


        private int? GetCurrentCustomerId()
        {
            string? idValue =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            return int.TryParse(
                idValue,
                out int id) &&
                id > 0
                    ? id
                    : null;
        }

        private static string? NormaliseOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
