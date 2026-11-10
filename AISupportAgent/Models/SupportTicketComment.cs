namespace AISupportAgent.Models
{
    public class SupportTicketComment
    {
        public int Id { get; set; }
        public string TicketId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Legacy comments are public and labeled "Legacy" during migration.
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorLabel { get; set; } = "Legacy";
        public bool IsInternal { get; set; }
        public SupportTicket Ticket { get; set; } = null!;
    }
}
