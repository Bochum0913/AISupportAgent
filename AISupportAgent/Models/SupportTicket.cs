namespace AISupportAgent.Models
{
    public class SupportTicket
    {
        public string Id { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public string Category { get; set; } = "Other";
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<SupportTicketComment> Comments { get; set; } = new();

    }
}
