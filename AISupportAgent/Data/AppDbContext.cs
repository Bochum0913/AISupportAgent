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

        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }

        public DbSet<SupportTicketComment> SupportTicketComments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<SupportTicketComment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId);
        }
    }
}
