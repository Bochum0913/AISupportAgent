using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AISupportAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly SupportTicketService _ticketService;

        public TicketsController(
            AppDbContext db,
            SupportTicketService ticketService)
        {
            _db = db;
            _ticketService = ticketService;
        }

        // GET /api/tickets
        [HttpGet]
        public async Task<IActionResult> GetTickets(
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? category)
        {
            var query = _db.SupportTickets.AsNoTracking();
            if (User.IsInRole("Customer"))
            {
                var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                query = query.Where(t => t.OwnerUserId == currentId);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            if (!string.IsNullOrWhiteSpace(priority))
                query = query.Where(t => t.Priority == priority);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(t => t.Category == category);

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(tickets);
        }

        // GET /api/tickets/INC-1001

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTicket(string id)
        {
            var visible = _db.SupportTickets.AsNoTracking().Where(t => t.Id == id);
            if (User.IsInRole("Customer"))
            {
                var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                visible = visible.Where(t => t.OwnerUserId == currentId);
            }
            if (!User.IsInRole("Customer") && !User.IsInRole("Agent") && !User.IsInRole("Admin"))
                return Forbid();

            var customerView = User.IsInRole("Customer");
            var ticket = await visible
                .Select(t => new
                {
                    t.Id,
                    t.Version,
                    t.Issue,
                    t.Status,
                    t.Priority,
                    t.Category,
                    t.AssignedTo,
                    t.CreatedAt,

                    Comments = t.Comments
                        .Where(c => !customerView || !c.IsInternal)
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Id,
                            c.Content,
                            c.CreatedAt,
                            c.AuthorLabel,
                            c.IsInternal
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (ticket == null)
            {
                return NotFound(new
                {
                    message = "Ticket not found."
                });
            }

            return Ok(ticket);
        }


        // GET /api/tickets/INC-1001/history
        [HttpGet("{id}/history")]
        public async Task<IActionResult> GetTicketHistory(string id)
        {
            var ticketQuery = _db.SupportTickets.AsNoTracking().Where(t => t.Id == id);
            if (User.IsInRole("Customer"))
            {
                var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ticketQuery = ticketQuery.Where(t => t.OwnerUserId == currentId);
            }
            else if (!User.IsInRole("Agent") && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (!await ticketQuery.AnyAsync())
                return NotFound(new { message = "Ticket not found." });

            var history = await _db.TicketAuditEntries.AsNoTracking()
                .Where(a => a.TicketId == id)
                .OrderByDescending(a => a.OccurredAtUtc)
                .ThenByDescending(a => a.Id)
                .Select(a => new
                {
                    a.Id,
                    a.FieldName,
                    a.OldValue,
                    a.NewValue,
                    a.ActorLabel,
                    a.OccurredAtUtc
                })
                .ToListAsync();
            return Ok(history);
        }

        // POST /api/tickets
        [HttpPost]
        public IActionResult CreateTicket(
            [FromBody] CreateTicketRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Issue))
                return BadRequest(new { message = "Issue is required." });

            string[] categories =
            {
                "Network", "Email", "Account",
                "Hardware", "Software", "Other"
            };

            if (string.IsNullOrWhiteSpace(request.Category) ||
                !categories.Contains(request.Category.Trim()))
                return BadRequest(new { message = "Invalid category." });

            var result = _ticketService.CreateTicket(
                request.Issue, request.Category, User.FindFirstValue(ClaimTypes.NameIdentifier));

            return Ok(new { message = result });
        }

        // PUT /api/tickets/INC-1001/status
        [Authorize(Roles = "Agent,Admin")]
        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(
            string id,
            [FromBody] UpdateStatusRequest request)
        {
            if (request.Version is null || request.Version < 1)
                return BadRequest(new { message = "A valid ticket version is required. Reload the ticket." });
            string[] validStatuses =
            {
                "Open", "In Progress", "Resolved"
            };

            if (string.IsNullOrWhiteSpace(request.Status) ||
                !validStatuses.Contains(request.Status.Trim()))
                return BadRequest(new { message = "Invalid status." });

            if (!_db.SupportTickets.Any(t => t.Id == id))
                return NotFound(new { message = "Ticket not found." });

            try
            {
                var result = _ticketService.UpdateTicketStatus(
                    id, request.Status,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name ?? "Staff", request.Version);
                return Ok(new { message = result });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "This ticket has been updated by someone else. Reload the ticket and try again." });
            }
        }

        // PUT /api/tickets/INC-1014/priority
        [Authorize(Roles = "Agent,Admin")]
        [HttpPut("{id}/priority")]
        public IActionResult UpdatePriority(
            string id,
            [FromBody] UpdatePriorityRequest request)
        {
            if (request.Version is null || request.Version < 1)
                return BadRequest(new { message = "A valid ticket version is required. Reload the ticket." });
            string[] validPriorities = { "Low", "Normal", "High" };

            if (string.IsNullOrWhiteSpace(request.Priority) ||
                !validPriorities.Contains(request.Priority.Trim()))
            {
                return BadRequest(new { message = "Invalid priority." });
            }

            if (!_db.SupportTickets.Any(t => t.Id == id))
            {
                return NotFound(new { message = "Ticket not found." });
            }

            try
            {
                var result = _ticketService.UpdateTicketPriority(
                    id, request.Priority.Trim(),
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name ?? "Staff", request.Version);
                return Ok(new { message = result });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "This ticket has been updated by someone else. Reload the ticket and try again." });
            }
        }

        // PUT /api/tickets/INC-1014/assignee
        [Authorize(Roles = "Agent,Admin")]
        [HttpPut("{id}/assignee")]
        public IActionResult AssignTicket(
            string id,
            [FromBody] AssignTicketRequest request)
        {
            if (request.Version is null || request.Version < 1)
                return BadRequest(new { message = "A valid ticket version is required. Reload the ticket." });
            if (string.IsNullOrWhiteSpace(request.AssignedTo))
            {
                return BadRequest(new { message = "Assigned To is required." });
            }

            if (!_db.SupportTickets.Any(t => t.Id == id))
            {
                return NotFound(new { message = "Ticket not found." });
            }

            try
            {
                var result = _ticketService.AssignTicket(
                    id, request.AssignedTo.Trim(),
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name ?? "Staff", request.Version);
                return Ok(new { message = result });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "This ticket has been updated by someone else. Reload the ticket and try again." });
            }
        }


        [HttpPost("{id}/comments")]
        public IActionResult AddComment(
            string id,
            [FromBody] AddCommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { message = "Comment cannot be empty." });
            if (request.Content.Trim().Length > 4000)
                return BadRequest(new { message = "Comment must be 4000 characters or fewer." });
            if (request.IsInternal && !User.IsInRole("Agent") && !User.IsInRole("Admin"))
                return Forbid();
            if (!User.IsInRole("Customer") && !User.IsInRole("Agent") && !User.IsInRole("Admin"))
                return Forbid();

            var commentTicketQuery = _db.SupportTickets.Where(t => t.Id == id);
            if (User.IsInRole("Customer"))
            {
                var currentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                commentTicketQuery = commentTicketQuery.Where(t => t.OwnerUserId == currentId);
            }
            if (!commentTicketQuery.Any())
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var result = _ticketService.AddComment(
                id, request.Content.Trim(),
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                User.Identity?.Name ?? "Unknown",
                request.IsInternal);

            return Ok(new { message = result });
        }


    }

    public class CreateTicketRequest
    {
        public string Issue { get; set; } = "";
        public string Category { get; set; } = "";
    }

    public class UpdateStatusRequest
    {
        public long? Version { get; set; }
        public string Status { get; set; } = "";
    }

    public class UpdatePriorityRequest
    {
        public long? Version { get; set; }
        public string Priority { get; set; } = "";
    }

    public class AssignTicketRequest
    {
        public long? Version { get; set; }
        public string AssignedTo { get; set; } = "";
    }

    public class AddCommentRequest
    {
        public string Content { get; set; } = "";
        public bool IsInternal { get; set; } = false;
    }


}
