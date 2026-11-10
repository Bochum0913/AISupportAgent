namespace AISupportAgent.Models;

/// <summary>Append-only record of a ticket field change.</summary>
public sealed class TicketAuditEntry
{
    public long Id { get; set; }
    public string TicketId { get; set; } = "";
    public string FieldName { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string ActorUserId { get; set; } = "";
    public string ActorLabel { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
