using AISupportAgent.Data;

namespace AISupportAgent.Services
{
    public class KnowledgeBaseService
    {
        private readonly AppDbContext _dbContext;

        public KnowledgeBaseService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string Search(string query)
        {
            var articles = _dbContext.KnowledgeArticles.ToList();

            foreach (var article in articles)
            {
                bool matches =
                    article.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    article.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    article.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    query.Contains(article.Title, StringComparison.OrdinalIgnoreCase) ||
                    query.Contains(article.Category, StringComparison.OrdinalIgnoreCase);

                if (matches)
                {
                    return $"Title: {article.Title}\n" +
                           $"Category: {article.Category}\n" +
                           $"Content: {article.Content}";
                }
            }

            return "No knowledge base article found.";
        }
    }
}