using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AISupportAgent.Tests;

// End-to-end HTTP authorization tests, using real Identity cookie auth and CSRF.
// Every test runs against isolated, temporary SQLite files; Docker data is untouched.
public sealed class RbacHttpTests
{
    private const string TestPassword = "TestPassword123!";

    private sealed class IsolatedApp : WebApplicationFactory<Program>
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "aisupport-rbac-" + Guid.NewGuid().ToString("N"));

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(_directory);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=" + Path.Combine(_directory, "tickets.db"),
                    ["ConnectionStrings:AuthConnection"] = "Data Source=" + Path.Combine(_directory, "identity.db"),
                    ["BootstrapAdmin:Email"] = "",
                    ["BootstrapAdmin:Password"] = "",
                    ["OpenAI:ApiKey"] = ""
                });
            });
        }

        public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        public async Task SeedAsync()
        {
            using var scope = Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            foreach (var (email, role) in new[]
            {
                ("customer@example.test", "Customer"),
                ("other@example.test", "Customer"),
                ("agent@example.test", "Agent"),
                ("otheragent@example.test", "Agent"),
                ("admin@example.test", "Admin")
            })
            {
                var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                Assert.True((await users.CreateAsync(user, TestPassword)).Succeeded);
                Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
            }

            var ticketService = scope.ServiceProvider.GetRequiredService<SupportTicketService>();
            var owner = (await users.FindByEmailAsync("customer@example.test"))!;
            var other = (await users.FindByEmailAsync("other@example.test"))!;
            var agent = (await users.FindByEmailAsync("agent@example.test"))!;
            ticketService.CreateTicket("Assigned ticket", "Software", owner.Id);
            ticketService.CreateTicket("Other customer's ticket", "Email", other.Id);
            ticketService.AssignTicket("INC-1001", agent.Id, "admin", "Admin", 1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            try { Directory.Delete(_directory, recursive: true); }
            catch (IOException) { /* Windows file-lock cleanup is best effort. */ }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task<string> CsrfAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()!;
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var csrf = await CsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = TestPassword })
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> PutAsync(HttpClient client, string path, object body)
    {
        var csrf = await CsrfAsync(client);
        using var request = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-CSRF-TOKEN", csrf);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task AnonymousUser_CannotListTickets()
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        using var response = await client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotReadAdminUserDirectoryOrAgentDirectory()
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        await app.SeedAsync();
        await LoginAsync(client, "customer@example.test");
        using var users = await client.GetAsync("/api/auth/users");
        using var agents = await client.GetAsync("/api/tickets/agents");
        Assert.Equal(HttpStatusCode.Forbidden, users.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, agents.StatusCode);
    }

    [Theory]
    [InlineData("customer@example.test")]
    [InlineData("agent@example.test")]
    public async Task NonAdmin_CannotAssignTickets(string email)
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        await app.SeedAsync();
        await LoginAsync(client, email);
        using var response = await PutAsync(client, "/api/tickets/INC-1002/assignee",
            new { version = 1, assignedTo = "someone" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Agent_CannotReadOrUpdateTicketAssignedToAnotherAgent()
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        await app.SeedAsync();
        await LoginAsync(client, "otheragent@example.test");
        using var read = await client.GetAsync("/api/tickets/INC-1001");
        using var update = await PutAsync(client, "/api/tickets/INC-1001/status",
            new { version = 2, status = "In Progress" });
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Customer_CannotReadOtherCustomersTicket()
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        await app.SeedAsync();
        await LoginAsync(client, "customer@example.test");
        using var response = await client.GetAsync("/api/tickets/INC-1002");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Agent_CannotSkipFromOpenDirectlyToResolved()
    {
        using var app = new IsolatedApp();
        using var client = app.NewClient();
        await app.SeedAsync();
        await LoginAsync(client, "agent@example.test");
        using var response = await PutAsync(client, "/api/tickets/INC-1001/status",
            new { version = 2, status = "Resolved", resolutionNote = "Some resolution" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
