using net_news_html.Models;

namespace net_news_html.Library.Interface;

public interface INewsStorage
{
    Task SaveNewsAsync(SavedNewsItem news);
    Task<List<SavedNewsItem>> GetNewsAsync(Guid? owner);
    Task DeleteNewsAsync(string url);
}

public interface INewsPersistenceStrorage : INewsStorage
{
    
}