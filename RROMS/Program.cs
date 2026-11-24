using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RROMS.Data;
using RROMS.Models;
using RROMS.Services;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. RAZOR PAGES
// ==========================================

builder.Services.AddRazorPages(options =>
{
    // Staff authorisation
    options.Conventions.AuthorizeFolder(
        "/Staff",
        "StaffOnly");

    options.Conventions.AllowAnonymousToPage(
        "/Staff/Login");

    options.Conventions.AllowAnonymousToPage(
        "/Staff/ForgotPassword");

    options.Conventions.AllowAnonymousToPage(
        "/Staff/ResetPassword");

    // Customer authorisation
    options.Conventions.AuthorizeFolder(
        "/Customer",
        "CustomerOnly");

    options.Conventions.AllowAnonymousToPage(
        "/Customer/Account/Login");

    options.Conventions.AllowAnonymousToPage(
        "/Customer/Account/Register");

    options.Conventions.AllowAnonymousToPage(
    "/Customer/Account/ForgotPassword");

    options.Conventions.AllowAnonymousToPage(
    "/Customer/Account/ResetPassword");

    options.Conventions.AllowAnonymousToPage(
        "/Customer/Dashboard/Index");

    // Administrator authorisation
    options.Conventions.AuthorizeFolder(
        "/Admin",
        "AdminOnly");

    options.Conventions.AllowAnonymousToPage(
        "/Account/Login");

    options.Conventions.AllowAnonymousToPage(
        "/Account/Logout");
});

// ==========================================
// 2. DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "DefaultConnection")));

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection(
        "SmtpSettings"));

// ==========================================
// 3. PASSWORD HASHING
// ==========================================

builder.Services.AddScoped<
    IPasswordHasher<RROMS.Models.Staff>,
    PasswordHasher<RROMS.Models.Staff>>();

builder.Services.AddScoped<
    IPasswordHasher<RROMS.Models.Customer>,
    PasswordHasher<RROMS.Models.Customer>>();

builder.Services.AddScoped<
    IPasswordHasher<RROMS.Models.Admin>,
    PasswordHasher<RROMS.Models.Admin>>();

builder.Services.AddScoped<
    IEmailService,
    GmailEmailService>();

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

        options.Events.OnRedirectToLogin = context =>
        {
            string loginPath =
                context.Request.Path.StartsWithSegments(
                    "/Customer")
                    ? "/Customer/Account/Login"
                    : context.Request.Path.StartsWithSegments(
                        "/Admin")
                        ? "/Account/Login"
                        : "/Staff/Login";

            string returnUrl = Uri.EscapeDataString(
                context.Request.PathBase +
                context.Request.Path +
                context.Request.QueryString);

            context.Response.Redirect(
                $"{context.Request.PathBase}" +
                $"{loginPath}?ReturnUrl={returnUrl}");

            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            string loginPath =
                context.Request.Path.StartsWithSegments(
                    "/Customer")
                    ? "/Customer/Account/Login"
                    : context.Request.Path.StartsWithSegments(
                        "/Admin")
                        ? "/Account/Login"
                        : "/Staff/Login";

            context.Response.Redirect(loginPath);

            return Task.CompletedTask;
        };

        options.Events.OnValidatePrincipal =
            async context =>
            {
                var role = context.Principal?
                    .FindFirstValue(ClaimTypes.Role);

                string? userIdValue = context.Principal?
                    .FindFirstValue(
                        ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(role) ||
                    !int.TryParse(
                        userIdValue,
                        out int userId) ||
                    userId <= 0)
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme);

                    return;
                }

                var db = context.HttpContext
                    .RequestServices
                    .GetRequiredService<
                        ApplicationDbContext>();

                if (role == "Staff")
                {
                    var staff = await db.Staff
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.StaffID == userId);

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
                }
                else if (role == "Customer")
                {
                    var customer = await db.Customers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.CustomerID == userId);

                    if (customer == null ||
                        !string.Equals(
                            customer.Status,
                            "Active",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return;
                    }

                    string? cookieName = context.Principal?
                        .FindFirstValue(ClaimTypes.Name);

                    string? cookieEmail = context.Principal?
                        .FindFirstValue(ClaimTypes.Email);

                    if (cookieName != customer.Name ||
                        cookieEmail != customer.Email)
                    {
                        var identity = new ClaimsIdentity(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        identity.AddClaim(new Claim(
                            ClaimTypes.NameIdentifier,
                            customer.CustomerID.ToString()));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Name,
                            customer.Name));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Email,
                            customer.Email));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Role,
                            "Customer"));

                        context.ReplacePrincipal(
                            new ClaimsPrincipal(identity));

                        context.ShouldRenew = true;
                    }
                }
                else if (role == "Admin")
                {
                    var admin = await db.Admins
                        .AsNoTracking()
                        .FirstOrDefaultAsync(item =>
                            item.AdminID == userId);

                    if (admin == null ||
                        !string.Equals(
                            admin.Role,
                            "Admin",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        context.RejectPrincipal();

                        await context.HttpContext.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return;
                    }

                    string? cookieName = context.Principal?
                        .FindFirstValue(ClaimTypes.Name);

                    string? cookieEmail = context.Principal?
                        .FindFirstValue(ClaimTypes.Email);

                    if (cookieName != admin.Name ||
                        cookieEmail != admin.Email)
                    {
                        var identity = new ClaimsIdentity(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        identity.AddClaim(new Claim(
                            ClaimTypes.NameIdentifier,
                            admin.AdminID.ToString()));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Name,
                            admin.Name));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Email,
                            admin.Email));

                        identity.AddClaim(new Claim(
                            ClaimTypes.Role,
                            "Admin"));

                        context.ReplacePrincipal(
                            new ClaimsPrincipal(identity));

                        context.ShouldRenew = true;
                    }
                }
                else
                {
                    context.RejectPrincipal();

                    await context.HttpContext.SignOutAsync(
                        CookieAuthenticationDefaults
                            .AuthenticationScheme);
                }
            };
    });

// ==========================================
// 5. AUTHORISATION
// ==========================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Staff");
    });

    options.AddPolicy("CustomerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Customer");
    });

    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

// ==========================================
// 6. SESSION
// ==========================================

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "Dineflow.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.Cookie.IsEssential = true;

    options.IdleTimeout =
        TimeSpan.FromHours(2);
});

// ==========================================
// 7. BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// 8. ERROR HANDLING
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ==========================================
// 9. MIDDLEWARE
// ==========================================

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ==========================================
// 10. STATIC FILES AND RAZOR PAGES
// ==========================================

app.MapStaticAssets();

app.MapRazorPages()
    .WithStaticAssets();

// ==========================================
// 11. START APPLICATION
// ==========================================


app.Run();