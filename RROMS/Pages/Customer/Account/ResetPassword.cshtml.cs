using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;

using CustomerModel = RROMS.Models.Customer;

namespace RROMS.Pages.Customer.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private readonly IPasswordHasher<CustomerModel>
            _passwordHasher;

        private readonly IEmailService _emailService;

        public ResetPasswordModel(
            ApplicationDbContext context,
            IPasswordHasher<CustomerModel> passwordHasher,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
        }

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; } =
            string.Empty;

        [BindProperty]
        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } =
            string.Empty;

        [BindProperty]
        [Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; } =
            string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public bool InvalidToken { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Token))
            {
                return NotFound();
            }

            PasswordResetToken? resetToken =
                await _context.PasswordResetTokens
                    .Include(token => token.Customer)
                    .FirstOrDefaultAsync(token =>
                        token.Token == Token);

            if (resetToken == null ||
    resetToken.Used ||
    resetToken.ExpiresAt < DateTime.Now)
            {
                InvalidToken = true;
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            PasswordResetToken? resetToken =
                await _context.PasswordResetTokens
                    .Include(token => token.Customer)
                    .FirstOrDefaultAsync(token =>
                        token.Token == Token);

            if (resetToken == null ||
    resetToken.Used ||
    resetToken.ExpiresAt < DateTime.Now)
            {
                InvalidToken = true;
                return Page();
            }

            CustomerModel customer =
                resetToken.Customer!;

            customer.Password =
                _passwordHasher.HashPassword(
                    customer,
                    NewPassword);

            resetToken.Used = true;

            await _context.SaveChangesAsync();

            string emailContent =
                $"""
        <p>Hello {customer.Name},</p>

        <p>
            Your Dineflow password has been
            reset successfully.
        </p>

        <p>
            If you did not perform this action,
            contact support immediately.
        </p>

        <p>
            Regards,<br/>
            Dineflow Security Team
        </p>
        """;

            await _emailService.SendEmailAsync(
                customer.Email,
                "Password Reset Successful",
                EmailTemplates.Wrap(
                    "Password Reset Successful",
                    emailContent));

            StatusMessage =
                "Your password has been reset successfully.";

            return RedirectToPage(
                "/Customer/Account/Login");
        }
    }
}