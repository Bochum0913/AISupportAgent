using System.Security.Claims;
using AISupportAgent.Controllers;
using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AISupportAgent.Tests;

public sealed class TicketTests
{
    // Each test uses its own disposable in-memory SQLite database; no local .db files touched.
    private sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public AppDbContext Db { get; }
        public SupportTicketService Service { get; }
        public TestDatabase()
        {
            _connection.Open();
            Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection).Options);
            Db.Database.EnsureCreated();
            Db.TicketNumberSequences.Add(new TicketNumberSequence { Id = 1, NextNumber = 1001 });
            Db.SaveChanges();
            Service = new SupportTicketService(Db);
        }
        public void Dispose() { Db.Dispose(); _connection.Dispose(); }
    }

    private static TicketsController AsUser(TestDatabase test, string id, string role)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, id + "@example.test"),
            new Claim(ClaimTypes.Role, role)
        }, "TestIdentity");
        return new TicketsController(test.Db, test.Service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    [Fact]
    public void CreateTicket_AssignsSequentialIdsAndOwner()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("First issue", "Software", "customer-1");
        test.Service.CreateTicket("Second issue", "Network", "customer-2");
        var tickets = test.Db.SupportTickets.OrderBy(t => t.Id).ToArray();
        Assert.Equal(new[] { "INC-1001", "INC-1002" }, tickets.Select(t => t.Id));
        Assert.Equal("customer-1", tickets[0].OwnerUserId);
        Assert.Equal("customer-2", tickets[1].OwnerUserId);
    }

    [Fact]
    public async Task Customer_CannotSeeAnotherCustomersTicketOrHistory()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("Private", "Email", "customer-1");
        test.Service.UpdateTicketPriority("INC-1001", "High", "admin", "admin@example.test", 1);
        var other = AsUser(test, "customer-2", "Customer");
        Assert.IsType<NotFoundObjectResult>(await other.GetTicket("INC-1001"));
        Assert.IsType<NotFoundObjectResult>(await other.GetTicketHistory("INC-1001"));
        var list = Assert.IsType<OkObjectResult>(await other.GetTickets(null, null, null));
        Assert.Empty(Assert.IsAssignableFrom<IEnumerable<SupportTicket>>(list.Value));
    }

    [Fact]
    public async Task Customer_CannotReadInternalComments()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("Email issue", "Email", "customer-1");
        test.Service.AddComment("INC-1001", "Public reply", "admin", "Admin", false);
        test.Service.AddComment("INC-1001", "Staff secret", "admin", "Admin", true);
        var result = Assert.IsType<OkObjectResult>(await AsUser(test, "customer-1", "Customer").GetTicket("INC-1001"));
        // Controller projects an anonymous response object. Inspect its Comments property.
        var comments = (System.Collections.IEnumerable)result.Value!.GetType().GetProperty("Comments")!.GetValue(result.Value)!;
        var items = comments.Cast<object>().ToArray();
        Assert.Single(items);
        Assert.Equal("Public reply", items[0].GetType().GetProperty("Content")!.GetValue(items[0]));
    }

    [Fact]
    public void StaleVersion_ThrowsWithoutChangingTicketOrAuditLog()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("Issue", "Software", "customer-1");
        test.Service.UpdateTicketPriority("INC-1001", "High", "agent", "Agent", 1);
        Assert.Throws<DbUpdateConcurrencyException>(() =>
            test.Service.UpdateTicketStatus("INC-1001", "Resolved", "agent", "Agent", 1));
        var ticket = test.Db.SupportTickets.Single();
        Assert.Equal("Open", ticket.Status);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal(2, ticket.Version);
        Assert.Single(test.Db.TicketAuditEntries);
    }

    [Fact]
    public void ValidUpdates_WriteAuditEntriesWithActorAndValues()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("Issue", "Software", "customer-1");
        test.Service.UpdateTicketStatus("INC-1001", "In Progress", "agent-1", "agent@example.test", 1);
        test.Service.AssignTicket("INC-1001", "Technician A", "agent-1", "agent@example.test", 2);
        var entries = test.Db.TicketAuditEntries.OrderBy(e => e.Id).ToArray();
        Assert.Equal(2, entries.Length);
        Assert.Equal(("Status", "Open", "In Progress"), (entries[0].FieldName, entries[0].OldValue, entries[0].NewValue));
        Assert.Equal("agent@example.test", entries[0].ActorLabel);
        Assert.Equal(("AssignedTo", "", "Technician A"), (entries[1].FieldName, entries[1].OldValue, entries[1].NewValue));
    }

    [Fact]
    public void Customer_CannotCreateInternalNote()
    {
        using var test = new TestDatabase();
        test.Service.CreateTicket("Issue", "Software", "customer-1");
        var result = AsUser(test, "customer-1", "Customer").AddComment("INC-1001", new AddCommentRequest
        {
            Content = "Trying an internal note", IsInternal = true
        });
        Assert.IsType<ForbidResult>(result);
        Assert.Empty(test.Db.SupportTicketComments);
    }
}
