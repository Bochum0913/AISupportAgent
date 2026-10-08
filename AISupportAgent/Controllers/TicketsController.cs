
using AISupportAgent.Data;
using AISupportAgent.Models;
using AISupportAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AISupportAgent.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
            var ticket = await _db.SupportTickets
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.Issue,
                    t.Status,
                    t.Priority,
                    t.Category,
                    t.AssignedTo,
                    t.CreatedAt,

                    Comments = t.Comments
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new
                        {
                            c.Content,
                            c.CreatedAt
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
                request.Issue, request.Category);

            return Ok(new { message = result });
        }

        // PUT /api/tickets/INC-1001/status
        [HttpPut("{id}/status")]
        public IActionResult UpdateStatus(
            string id,
            [FromBody] UpdateStatusRequest request)
        {
            string[] validStatuses =
            {
                "Open", "In Progress", "Resolved"
            };

            if (string.IsNullOrWhiteSpace(request.Status) ||
                !validStatuses.Contains(request.Status.Trim()))
                return BadRequest(new { message = "Invalid status." });

            if (!_db.SupportTickets.Any(t => t.Id == id))
                return NotFound(new { message = "Ticket not found." });

            var result = _ticketService.UpdateTicketStatus(
                id, request.Status);

            return Ok(new { message = result });
        }

        // PUT /api/tickets/INC-1014/priority
        [HttpPut("{id}/priority")]
        public IActionResult UpdatePriority(
            string id,
            [FromBody] UpdatePriorityRequest request)
        {
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

            var result = _ticketService.UpdateTicketPriority(
                id, request.Priority.Trim());

            return Ok(new { message = result });
        }

        // PUT /api/tickets/INC-1014/assignee
        [HttpPut("{id}/assignee")]
        public IActionResult AssignTicket(
            string id,
            [FromBody] AssignTicketRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AssignedTo))
            {
                return BadRequest(new { message = "Assigned To is required." });
            }

            if (!_db.SupportTickets.Any(t => t.Id == id))
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var result = _ticketService.AssignTicket(
                id, request.AssignedTo.Trim());

            return Ok(new { message = result });
        }


        [HttpPost("{id}/comments")]
        public IActionResult AddComment(
            string id,
            [FromBody] AddCommentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Comment cannot be empty." });
            }

            if (!_db.SupportTickets.Any(t => t.Id == id))
            {
                return NotFound(new { message = "Ticket not found." });
            }

            var result = _ticketService.AddComment(
                id, request.Content.Trim());

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
        public string Status { get; set; } = "";
    }

    public class UpdatePriorityRequest
    {
        public string Priority { get; set; } = "";
    }

    public class AssignTicketRequest
    {
        public string AssignedTo { get; set; } = "";
    }

    public class AddCommentRequest
    {
        public string Content { get; set; } = "";
    }


}
