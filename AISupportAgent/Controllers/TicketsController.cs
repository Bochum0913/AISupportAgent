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
        private readonly AuthDbContext? _authDb;
        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private bool IsAdmin => User.IsInRole("Admin");
        private bool IsAgent => User.IsInRole("Agent") && !IsAdmin;
        private bool IsCustomer => User.IsInRole("Customer") && !IsAdmin && !IsAgent;
        private bool CanStaffEdit(SupportTicket t) => IsAdmin || (IsAgent && t.AssignedTo == CurrentUserId);

        public TicketsController(
            AppDbContext db,
            SupportTicketService ticketService, AuthDbContext? authDb = null)
        {
            _db = db;
            _ticketService = ticketService;
            _authDb = authDb;
        }

        // GET /api/tickets
        [HttpGet]
        public async Task<IActionResult> GetTickets(
            [FromQuery] string? status,
            [FromQuery] string? priority,
            [FromQuery] string? category)
        {
            var query = _db.SupportTickets.AsNoTracking();
            if (IsCustomer) query = query.Where(t => t.OwnerUserId == CurrentUserId);
            else if (IsAgent) query = query.Where(t => t.AssignedTo == CurrentUserId);
            else if (!IsAdmin) return Forbid();

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
            if (IsCustomer) visible = visible.Where(t => t.OwnerUserId == CurrentUserId);
            else if (IsAgent) visible = visible.Where(t => t.AssignedTo == CurrentUserId);
            if (!IsCustomer && !IsAgent && !IsAdmin) return Forbid();

            var customerView = IsCustomer;
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
            if (IsCustomer) ticketQuery = ticketQuery.Where(t => t.OwnerUserId == CurrentUserId);
            else if (IsAgent) ticketQuery = ticketQuery.Where(t => t.AssignedTo == CurrentUserId);
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

            var target = _db.SupportTickets.AsNoTracking().FirstOrDefault(t => t.Id == id);
            if (target is null) return NotFound(new { message = "Ticket not found." });
            if (!CanStaffEdit(target)) return Forbid();
            if (request.Status == "Resolved" && string.IsNullOrWhiteSpace(request.ResolutionNote))
                return BadRequest(new { message = "A resolution note is required before resolving." });
            if (request.ResolutionNote?.Length > 4000)
                return BadRequest(new { message = "Resolution note is too long." });

            try
            {
                var result = _ticketService.UpdateTicketStatus(
                    id, request.Status,
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    User.Identity?.Name ?? "Staff", request.Version, request.ResolutionNote);
                return Ok(new { message = result });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
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

            var target = _db.SupportTickets.AsNoTracking().FirstOrDefault(t => t.Id == id);
            if (target is null) return NotFound(new { message = "Ticket not found." });
            if (!CanStaffEdit(target)) return Forbid();

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
        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/assignee")]
        public IActionResult AssignTicket(
            string id,
            [FromBody] AssignTicketRequest request)
        {
            if (request.Version is null || request.Version < 1)
                return BadRequest(new { message = "A valid ticket version is required. Reload the ticket." });
            if (string.IsNullOrWhiteSpace(request.AssignedTo))
                return BadRequest(new { message = "Agent user ID is required." });
            if (_authDb is null) return StatusCode(500, new { message = "Identity database is unavailable." });
            var agentId = request.AssignedTo.Trim();
            var validAgent = (from u in _authDb.Users
                              join ur in _authDb.UserRoles on u.Id equals ur.UserId
                              join r in _authDb.Roles on ur.RoleId equals r.Id
                              where u.Id == agentId && r.NormalizedName == "AGENT"
                              select u.Id).Any();
            if (!validAgent) return BadRequest(new { message = "Select an existing Agent account." });

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


        // Admin uses verified Identity IDs, not free-text assignee names.
        [Authorize(Roles = "Admin")]
        [HttpGet("agents")]
        public async Task<IActionResult> GetAgents()
        {
            if (_authDb is null) return StatusCode(500);
            var agents = await (from u in _authDb.Users
                                join ur in _authDb.UserRoles on u.Id equals ur.UserId
                                join r in _authDb.Roles on ur.RoleId equals r.Id
                                where r.NormalizedName == "AGENT"
                                orderby u.Email
                                select new { id = u.Id, email = u.Email }).ToListAsync();
            return Ok(agents);
        }

        // Only the ticket owner decides whether a resolved ticket is closed or reopened.
        [Authorize(Roles = "Customer")]
        [HttpPut("{id}/confirmation")]
        public IActionResult ConfirmResolution(string id, [FromBody] ConfirmResolutionRequest request)
        {
            if (request.Version is null || request.Version < 1)
                return BadRequest(new { message = "Reload the ticket to get the current version." });
            var ticket = _db.SupportTickets.AsNoTracking().FirstOrDefault(t => t.Id == id && t.OwnerUserId == CurrentUserId);
            if (ticket is null) return NotFound(new { message = "Ticket not found." });
            if (ticket.Status != "Resolved") return BadRequest(new { message = "Only resolved tickets can be confirmed." });
            var status = request.Resolved ? "Closed" : "Reopened";
            try
            {
                var result = _ticketService.UpdateTicketStatus(id, status, CurrentUserId,
                    User.Identity?.Name ?? "Customer", request.Version);
                return Ok(new { message = result });
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Ticket was updated. Reload and retry." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
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
            if (IsCustomer) commentTicketQuery = commentTicketQuery.Where(t => t.OwnerUserId == CurrentUserId);
            else if (IsAgent) commentTicketQuery = commentTicketQuery.Where(t => t.AssignedTo == CurrentUserId);
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
        public string? ResolutionNote { get; set; }
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

    public class ConfirmResolutionRequest
    {
        public long? Version { get; set; }
        public bool Resolved { get; set; }
    }

    public class AddCommentRequest
    {
        public string Content { get; set; } = "";
        public bool IsInternal { get; set; } = false;
    }


}
