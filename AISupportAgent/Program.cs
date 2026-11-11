using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
// Customer AI endpoint: prevent unlimited paid API requests from one account.
builder.Services.AddHttpClient("openai-self-service", client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("customer-ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Identity runs in its own SQLite file, keeping old ticket data intact.
builder.Services.AddDbContext<AuthDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("AuthConnection") ?? "Data Source=auth.db"));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
}).AddEntityFrameworkStores<AuthDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/api/auth/login";
    options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<KnowledgeBaseService>();
builder.Services.AddScoped<SupportTicketService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MicrosoftGraphService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
var trustedProxy = builder.Configuration["ReverseProxy:KnownProxy"];

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto
};

if (!string.IsNullOrWhiteSpace(trustedProxy))
{
    if (!System.Net.IPAddress.TryParse(trustedProxy, out var address))
    {
        throw new InvalidOperationException(
            "Invalid ReverseProxy:KnownProxy configuration.");
    }

    forwardedHeadersOptions.KnownProxies.Add(address);
}

app.UseForwardedHeaders(forwardedHeadersOptions);

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    var authDb = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    authDb.Database.EnsureCreated();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Customer", "Agent", "Admin" })
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));

    // Optional initial admin. NEVER commit a password to Git; set both env vars locally.
    var adminEmail = builder.Configuration["BootstrapAdmin:Email"];
    var adminPassword = builder.Configuration["BootstrapAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await users.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            var create = await users.CreateAsync(admin, adminPassword);
            if (!create.Succeeded) throw new InvalidOperationException("Admin bootstrap failed: " +
                string.Join(", ", create.Errors.Select(e => e.Description)));
        }
        if (!await users.IsInRoleAsync(admin, "Admin"))
        {
            // Do not elevate an existing public-registration account via bootstrap email.
            if (await users.GetRolesAsync(admin) is { Count: > 0 })
                throw new InvalidOperationException("Bootstrap email belongs to a non-admin user. Use a fresh admin email.");
            await users.AddToRoleAsync(admin, "Admin");
        }
    }

    if (!dbContext.KnowledgeArticles.Any(a => a.Title == "VPN Troubleshooting"))
    {
        dbContext.KnowledgeArticles.Add(
            new KnowledgeArticle
            {
                Title = "VPN Troubleshooting",
                Category = "VPN",
                Content = "Restart the VPN client and verify your company credentials and MFA."
            }
        );
    }

    if (!dbContext.KnowledgeArticles.Any(a => a.Title == "Outlook Email Troubleshooting"))
    {
        dbContext.KnowledgeArticles.Add(
            new KnowledgeArticle
            {
                Title = "Outlook Email Troubleshooting",
                Category = "Email",
                Content = "Restart Outlook, verify your internet connection, and check your account settings."
            }
        );
    }

    if (!dbContext.KnowledgeArticles.Any(a => a.Title == "Password Reset"))
    {
        dbContext.KnowledgeArticles.Add(
            new KnowledgeArticle
            {
                Title = "Password Reset",
                Category = "Account",
                Content = "Use the company password reset portal to reset your password and verify your identity."
            }
        );
    }

    dbContext.SaveChanges();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Double-submit antiforgery token checked server-side for ALL modifying /api requests.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        if (!await antiforgery.IsRequestValidAsync(context))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid or missing CSRF token." });
            return;
        }
    }
    await next();
});

app.MapControllers();

app.Run();

public partial class Program { }