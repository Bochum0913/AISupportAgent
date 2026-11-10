namespace AISupportAgent.Models;

// A single SQLite row allocates ticket numbers transactionally.
public sealed class TicketNumberSequence
{
    public int Id { get; set; } = 1;
    public long NextNumber { get; set; }
}
