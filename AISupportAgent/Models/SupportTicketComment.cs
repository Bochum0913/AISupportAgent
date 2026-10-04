namespace AISupportAgent.Models
{
    public class SupportTicketComment
    {
        public int Id { get; set; }

        public string TicketId { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public SupportTicket Ticket { get; set; } = null!;
    }
}