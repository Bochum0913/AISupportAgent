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
        public string CreateTicket(string issue, string category, string? ownerUserId = null)
        {
            if (string.IsNullOrWhiteSpace(issue))
            {
                return "Issue description cannot be empty.";
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

            // SQLite allows only one writer at a time. The number reservation and
            // ticket insert are in ONE transaction: a failed insert rolls back both.
            // The counter is seeded from existing INC-xxxx IDs by the migration.
            using var transaction = _dbContext.Database.BeginTransaction();
            var changed = _dbContext.Database.ExecuteSqlRaw(
                "UPDATE \"TicketNumberSequences\" SET \"NextNumber\" = \"NextNumber\" + 1 WHERE \"Id\" = 1");
            if (changed != 1)
                throw new InvalidOperationException("Ticket number generator has not been initialized. Apply migrations.");

            // Value is the alias required by EF for a scalar SQL query.
            var number = _dbContext.Database.SqlQueryRaw<long>(
                "SELECT \"NextNumber\" - 1 AS \"Value\" FROM \"TicketNumberSequences\" WHERE \"Id\" = 1")
                .Single();

            SupportTicket ticket = new SupportTicket
            {
                Id = $"INC-{number}",
                Version = 1,
                OwnerUserId = ownerUserId,
                Issue = issue.Trim(),
                Status = "Open",
                Priority = "Normal",
                Category = normalizedCategory,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.SupportTickets.Add(ticket);
            _dbContext.SaveChanges();
            transaction.Commit();

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
                         ticket.Comments.Where(c => !c.IsInternal).OrderBy(c => c.CreatedAt))
                {
                    result += $"\n- {comment.Content}";
                }
            }

            return result;
        }
        // For browser writes, reject a version read before somebody else's update.
        // Agent tools may pass null, but still bump Version so browser edits detect them.
        private static void VerifyVersion(SupportTicket ticket, long? expectedVersion)
        {
            if (expectedVersion.HasValue && ticket.Version != expectedVersion.Value)
                throw new DbUpdateConcurrencyException("Ticket was changed by another user. Reload and retry.");
        }

        private void RecordChange(string ticketId, string field, string oldValue, string newValue,
            string? actorUserId, string? actorLabel)
        {
            _dbContext.TicketAuditEntries.Add(new TicketAuditEntry
            {
                TicketId = ticketId,
                FieldName = field,
                OldValue = oldValue ?? "",
                NewValue = newValue,
                ActorUserId = actorUserId ?? "",
                // Calls originating outside authenticated ticket endpoints (e.g. the AI tool)
                // must not be attributed to the currently signed-in admin automatically.
                ActorLabel = string.IsNullOrWhiteSpace(actorLabel) ? "AI/Service" : actorLabel,
                OccurredAtUtc = DateTime.UtcNow
            });
        }

        public string UpdateTicketStatus(string ticketId, string status, string? actorUserId = null, string? actorLabel = null, long? expectedVersion = null)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            VerifyVersion(ticket, expectedVersion);

            string normalizedStatus = status.Trim();

            if (normalizedStatus != "Open" &&
                normalizedStatus != "In Progress" &&
                normalizedStatus != "Resolved")
            {
                return "Invalid status. Status must be Open, In Progress, or Resolved.";
            }

            if (ticket.Status == normalizedStatus)
                return $"Support ticket {ticketId} status is already {normalizedStatus}.";

            RecordChange(ticket.Id, "Status", ticket.Status, normalizedStatus, actorUserId, actorLabel);
            ticket.Status = normalizedStatus;
            ticket.Version++;
            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} status updated to {normalizedStatus}.";
        }
        public string UpdateTicketPriority(string ticketId, string priority, string? actorUserId = null, string? actorLabel = null, long? expectedVersion = null)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            VerifyVersion(ticket, expectedVersion);

            string normalizedPriority = priority.Trim();

            if (normalizedPriority != "Low" &&
                normalizedPriority != "Normal" &&
                normalizedPriority != "High")
            {
                return "Invalid priority. Priority must be Low, Normal, or High.";
            }

            if (ticket.Priority == normalizedPriority)
                return $"Support ticket {ticketId} priority is already {normalizedPriority}.";

            RecordChange(ticket.Id, "Priority", ticket.Priority, normalizedPriority, actorUserId, actorLabel);
            ticket.Priority = normalizedPriority;
            ticket.Version++;
            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} priority updated to {normalizedPriority}.";
        }
        public string AssignTicket(string ticketId, string assignedTo, string? actorUserId = null, string? actorLabel = null, long? expectedVersion = null)
        {
            SupportTicket? ticket = _dbContext.SupportTickets
                .FirstOrDefault(t => t.Id == ticketId);

            if (ticket == null)
            {
                return $"Support ticket {ticketId} was not found.";
            }

            VerifyVersion(ticket, expectedVersion);

            if (string.IsNullOrWhiteSpace(assignedTo))
            {
                return "Assigned technician name cannot be empty.";
            }

            var newAssignee = assignedTo.Trim();
            if (ticket.AssignedTo == newAssignee)
                return $"Support ticket {ticketId} is already assigned to {newAssignee}.";

            RecordChange(ticket.Id, "AssignedTo", ticket.AssignedTo, newAssignee, actorUserId, actorLabel);
            ticket.AssignedTo = newAssignee;
            ticket.Version++;
            _dbContext.SaveChanges();

            return $"Support ticket {ticketId} assigned to {ticket.AssignedTo}.";
        }
        public string AddComment(string ticketId, string content, string? authorUserId = null, string? authorLabel = null, bool isInternal = false)
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
                CreatedAt = DateTime.UtcNow,
                AuthorUserId = authorUserId ?? "",
                AuthorLabel = string.IsNullOrWhiteSpace(authorLabel) ? "AI/Service" : authorLabel,
                IsInternal = isInternal
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
