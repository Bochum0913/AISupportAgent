using AISupportAgent.Models;
using Microsoft.EntityFrameworkCore;

namespace AISupportAgent.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<ConversationMessage> ConversationMessages { get; set; }

        public DbSet<SupportTicket> SupportTickets { get; set; }

        public DbSet<TicketNumberSequence> TicketNumberSequences { get; set; }

        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }

        public DbSet<SupportTicketComment> SupportTicketComments { get; set; }

        public DbSet<TicketAuditEntry> TicketAuditEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TicketNumberSequence>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Id).ValueGeneratedNever();
                entity.Property(s => s.NextNumber).IsRequired();
            });

            modelBuilder.Entity<SupportTicket>(entity =>
            {
                // SQLite has no SQL Server-style rowversion; use an application-managed token.
                entity.Property(t => t.Version).IsConcurrencyToken().HasDefaultValue(1L);
            });

            modelBuilder.Entity<TicketAuditEntry>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TicketId, e.OccurredAtUtc });
                entity.Property(e => e.TicketId).IsRequired();
                entity.Property(e => e.FieldName).IsRequired();
                entity.Property(e => e.ActorUserId).IsRequired();
                entity.Property(e => e.ActorLabel).IsRequired();
            });

            modelBuilder.Entity<SupportTicketComment>(entity =>
            {
                entity.Property(c => c.AuthorUserId).IsRequired();
                entity.Property(c => c.AuthorLabel).IsRequired();
                entity.Property(c => c.IsInternal).IsRequired();
                entity.HasOne(c => c.Ticket)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TicketId);
            });
        }
    }
}
