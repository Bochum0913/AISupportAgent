using AISupportAgent.Data;
using AISupportAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace AISupportAgent.Services
{
    public class SupportTicketService
    {
        private readonly AppDbContext _dbContext;

        public SupportTicketService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public string CreateTicket(string issue, string category)
        {
            if (string.IsNullOrWhiteSpace(issue))
            {
                return "Issue description cannot be empty.";
            }

            int nextNumber = 1001;

            if (_dbContext.SupportTickets.Any())
            {
                string lastId = _dbContext.SupportTickets
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.Id)
                    .First();

                string numberPart = lastId.Replace("INC-", "");
                nextNumber = int.Parse(numberPart) + 1;
            }

            string normalizedCategory = category.Trim();

            if (normalizedCategory != "Network" &&
                normalizedCategory != "Email" &&
                normalizedCategory != "Account" &&
                normalizedCategory != "Hardware" &&
                normalizedCategory != "Software" &&
                normalizedCategory != "Other")
            {
                return "Invalid category. Category must be Network, Email, Account, Hardware, Software, or Other.";
            }

            SupportTicket ticket = new SupportTicket
            {
                Id = "INC-" + nextNumber,
                Issue = issue.Trim(),
                Status = "Open",
                Priority = "Normal",
                Category = normalizedCategory,
                CreatedAt = DateTime.Now
            };

            _dbContext.SupportTickets.Add(ticket);
            _dbContext.SaveChanges();

            return ticket.Id +
                   " created successfully for: " + issue +
                   " Category: " + normalizedCategory;
        }
        public string GetTicket(string ticketId)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .Include(t => t.Comments)
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            string assignedTo = string.IsNullOrWhiteSpace(ticket.AssignedTo) ? "Unassigned" : ticket.AssignedTo;

            string result =
                $"Ticket ID: {ticket.Id}\n" +
                $"Issue: {ticket.Issue}\n" +
                $"Status: {ticket.Status}\n" +
                $"Priority: {ticket.Priority}\n" +
                $"Category: {ticket.Category}\n" +
                $"Assigned To: {assignedTo}\n" +
                $"Created At: {ticket.CreatedAt}";

            if (ticket.Comments.Count > 0)
            {
                result += "\nComments:";

                foreach (SupportTicketComment comment in
                         ticket.Comments.OrderBy(c => c.CreatedAt))
                {
                    result += $"\n- {comment.Content}";
                }
            }

            return result;
        }
        public string UpdateTicketStatus(string ticketId, string status)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            string normalizedStatus = status.Trim();

            if (normalizedStatus != "Open" &&
                normalizedStatus != "In Progress" &&
                normalizedStatus != "Resolved")
            {
                return "Invalid status. Status must be Open, In Progress, or Resolved.";
            }

            ticket.Status = normalizedStatus;

            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} status updated to {normalizedStatus}.";
        }
        public string UpdateTicketPriority(string ticketId, string priority)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            string normalizedPriority = priority.Trim();

            if (normalizedPriority != "Low" &&
                normalizedPriority != "Normal" &&
                normalizedPriority != "High")
            {
                return "Invalid priority. Priority must be Low, Normal, or High.";
            }

            ticket.Priority = normalizedPriority;

            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} priority updated to {normalizedPriority}.";
        }
        public string AssignTicket(string ticketId, string assignedTo)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            if (string.IsNullOrWhiteSpace(assignedTo))
            {
                return "Assigned technician name cannot be empty.";
            }

            ticket.AssignedTo = assignedTo.Trim();

            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} assigned to {ticket.AssignedTo}.";
        }
        public string AddComment(string ticketId, string content)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return "Comment content cannot be empty.";
            }

            SupportTicketComment comment = new SupportTicketComment
            {
                TicketId = ticketId,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _dbContext.SupportTicketComments.Add(comment);
            _dbContext.SaveChanges();

            return $"Comment added successfully to ticket {ticketId}.";
        }

        public string ListTickets(string? status, string? priority, string? category)
        {
            IQueryable<SupportTicket> query = _dbContext.SupportTickets;

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(t => t.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            List<SupportTicket> tickets = query
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            if (tickets.Count == 0)
            {
                return "No support tickets were found matching the filters.";
            }

            string result = "";

            foreach (SupportTicket ticket in tickets)
            {
                result +=
                    $"Ticket ID: {ticket.Id}\n" +
                    $"Issue: {ticket.Issue}\n" +
                    $"Status: {ticket.Status}\n" +
                    $"Priority: {ticket.Priority}\n" +
                    $"Category: {ticket.Category}\n\n";
            }

            return result;
        }
    }
}
