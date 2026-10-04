using AISupportAgent.Data;
using AISupportAgent.Models;

namespace AISupportAgent.Services
{
    public class ConversationService
    {
        private readonly AppDbContext _dbContext;

        public ConversationService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void AddMessage(
            string conversationId,
            string role,
            string content)
        {
            ConversationMessage message = new ConversationMessage
            {
                ConversationId = conversationId,
                Role = role,
                Content = content
            };

            _dbContext.ConversationMessages.Add(message);

            _dbContext.SaveChanges();
        }

        public List<ConversationMessage> GetMessages(string conversationId)
        {
            return _dbContext.ConversationMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt)
                .ToList();
        }
    }
}