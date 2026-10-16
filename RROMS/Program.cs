
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. RAZOR PAGES
// ==========================================

builder.Services.AddRazorPages(options =>
{
    // Only authenticated staff can access Staff pages
    options.Conventions.AuthorizeFolder(
        "/Staff", "StaffOnly");

    // Login must remain accessible without signing in
    options.Conventions.AllowAnonymousToPage(
        "/Staff/Login");
});

// ==========================================
// 2. DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

// ==========================================
// 3. PASSWORD HASHING
// ==========================================

builder.Services.AddScoped<
    IPasswordHasher<RROMS.Models.Staff>,
    PasswordHasher<RROMS.Models.Staff>>();

// ==========================================
// 4. COOKIE AUTHENTICATION
// ==========================================

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Staff/Login";
        options.AccessDeniedPath = "/Staff/Login";

        options.Cookie.Name = "Dineflow.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;
        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        // Validate account whenever its cookie is checked
        options.Events.OnValidatePrincipal = async context =>
        {
            string? staffIdValue =
                context.Principal?.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            bool isStaffRole =
                context.Principal?.IsInRole("Staff") == true;

            if (!isStaffRole ||
                !int.TryParse(staffIdValue, out int staffId) ||
                staffId <= 0)
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

                return;
            }

            var db = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            var staff = await db.Staff
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.StaffID == staffId);

            // Reject deleted, disabled or reassigned accounts
            if (staff == null ||
                !string.Equals(
                    staff.Status,
                    "Active",
                    StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(
                    staff.Role,
                    "Staff",
                    StringComparison.OrdinalIgnoreCase))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

                return;
            }

            // Keep displayed name/email consistent with database
            string? cookieName = context.Principal?
                .FindFirstValue(ClaimTypes.Name);

            string? cookieEmail = context.Principal?
                .FindFirstValue(ClaimTypes.Email);

            if (cookieName != staff.Name ||
                cookieEmail != staff.Email)
            {
                var identity = new ClaimsIdentity(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

                identity.AddClaim(new Claim(
                    ClaimTypes.NameIdentifier,
                    staff.StaffID.ToString()));

                identity.AddClaim(new Claim(
                    ClaimTypes.Name,
                    staff.Name));

                identity.AddClaim(new Claim(
                    ClaimTypes.Email,
                    staff.Email));

                identity.AddClaim(new Claim(
                    ClaimTypes.Role,
                    "Staff"));

                context.ReplacePrincipal(
                    new ClaimsPrincipal(identity));

                context.ShouldRenew = true;
            }
        };
    });

// ==========================================
// 5. AUTHORIZATION
// ==========================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Staff");
    });
});

// ==========================================
// 6. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 7. ERROR HANDLING
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ==========================================
// 8. MIDDLEWARE
// ==========================================

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ==========================================
// 9. STATIC FILES & RAZOR PAGES
// ==========================================

app.MapStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// ==========================================
// 10. START APPLICATION
// ==========================================

app.Run();
