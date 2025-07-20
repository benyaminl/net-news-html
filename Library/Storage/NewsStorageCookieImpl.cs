using System.Text.Json;
using net_news_html.Library.Interface;
using net_news_html.Models;

namespace net_news_html.Library.Storage;

public class NewsStorageCookieImpl : INewsStorage 
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NewsStorageCookieImpl(
        IHttpContextAccessor httpContextAccessor
    )
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task DeleteNewsAsync(string url)
    {
        var data = await GetNewsAsync();
        data.RemoveAll(x => x.Url == url);
        string cookie = JsonSerializer.Serialize(data);
        _httpContextAccessor.HttpContext?.Response.Cookies.Append("news", cookie, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(300),
            SameSite = SameSiteMode.Strict,
            HttpOnly = false
        });
        
    }

    public async Task<List<SavedNewsItem>> GetNewsAsync(Guid? owner = null)
    {
        string cookie = _httpContextAccessor.HttpContext?.Request.Cookies["news"] ?? "[]";

        List<SavedNewsItem> news = JsonSerializer.Deserialize<List<SavedNewsItem>>(cookie) ?? new List<SavedNewsItem>();

        return await Task.FromResult(news);
    }

    public async Task SaveNewsAsync(SavedNewsItem item)
    {
        List<SavedNewsItem> news = await GetNewsAsync();
        news.Add(item);

        string cookie = JsonSerializer.Serialize(news);

        _httpContextAccessor.HttpContext?.Response.Cookies.Append("news", cookie, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(300),
            SameSite = SameSiteMode.Strict,
            HttpOnly = false
        });
    }
}