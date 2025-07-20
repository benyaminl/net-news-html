using Microsoft.EntityFrameworkCore;
using net_news_html.Library.Interface;
using net_news_html.Models;

namespace net_news_html.Library.Storage
{
    public class NewsStorageSqliteImpl : INewsPersistenceStrorage
    {
        private readonly NewsDbContext _dbContext;

        public NewsStorageSqliteImpl(NewsDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbContext.Database.EnsureCreated();
        }

        public async Task DeleteNewsAsync(string url)
        {
            var news = await _dbContext.SavedNews.FindAsync(url);
            if (news != null)
            {
                _dbContext.SavedNews.Remove(news);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<SavedNewsItem>> GetNewsAsync(Guid? owner = null)
        {
            var query = _dbContext.SavedNews
                .OrderByDescending(n => n.SaveDate);

            if (owner != null)
            {
                query = (IOrderedQueryable<SavedNewsItem>) query.Where(n => n.PassKeyId == owner!);
            }

            return await query.ToListAsync();
        }

        public async Task SaveNewsAsync(SavedNewsItem news)
        {
            var existingNews = await _dbContext.SavedNews.FindAsync(news.Url);
            if (existingNews == null)
            {
                await _dbContext.SavedNews.AddAsync(news);
            }
            else
            {
                existingNews.Title = news.Title;
                existingNews.SaveDate = news.SaveDate;
                _dbContext.SavedNews.Update(existingNews);
            }
            
            await _dbContext.SaveChangesAsync();
        }
    }
}