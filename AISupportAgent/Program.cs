using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddScoped<AgentService>();
builder.Services.AddScoped<KnowledgeBaseService>();
builder.Services.AddScoped<SupportTicketService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MicrosoftGraphService>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
