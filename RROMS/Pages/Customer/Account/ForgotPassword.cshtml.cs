using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;
using CustomerModel = RROMS.Models.Customer;

namespace RROMS.Pages.Customer.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private readonly IEmailService _emailService;

        public ForgotPasswordModel(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        [Required]
        [EmailAddress]
        public string Email { get; set; } =
            string.Empty;

        [TempData]
        public string? StatusMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            CustomerModel? customer =
                        await _context.Customers
                    .FirstOrDefaultAsync(customer =>
                        customer.Email == Email);

            if (customer != null)
            {
                string token =
                    Convert.ToHexString(
                        RandomNumberGenerator.GetBytes(32));

                var resetToken =
                    new PasswordResetToken
                    {
                        CustomerID =
                            customer.CustomerID,

                        Token =
                            token,

                        ExpiresAt =
                            DateTime.Now.AddMinutes(30),

                        Used =
                            false
                    };

                _context.PasswordResetTokens
                    .Add(resetToken);

                await _context.SaveChangesAsync();

                string resetUrl =
                    Url.Page(
                        "/Customer/Account/ResetPassword",
                        null,
                        new
                        {
                            token = token
                        },
                        Request.Scheme)
                    ?? "#";

                string emailContent =
                    $"""
                    <p>Hello {customer.Name},</p>

                    <p>
                        We received a request to
                        reset your Dineflow password.
                    </p>

                    <div style="
                        background:#f9fafb;
                        border-radius:10px;
                        padding:14px;
                        margin:18px 0;">

                        <a href="{resetUrl}"
                       style="
                            display:inline-block;
                            padding:12px 24px;
                            background:#f59e0b;
                            color:white;
                            text-decoration:none;
                            border-radius:8px;
                            font-weight:700;">
                        Reset Password
                    </a>
                    </div>

                    <p>
                        This link expires in
                        30 minutes.
                    </p>

                    <p>
                        If you did not request
                        a password reset, you may
                        safely ignore this email.
                    </p>

                    <p>
                        Regards,<br/>
                        Dineflow Security Team
                    </p>
                    """;

                await _emailService.SendEmailAsync(
                    customer.Email,
                    "Reset Your Dineflow Password",
                    EmailTemplates.Wrap(
                        "Password Reset Request",
                        emailContent));
            }

            StatusMessage =
                "If an account exists with that email address, a reset link will be sent.";

            return RedirectToPage();
        }
    }
}