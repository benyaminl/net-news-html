using net_news_html.Models;

namespace net_news_html.Library.Interface;

public interface IParserService
{
    public void SetListUrl(string url);
    public string GetListUrl();
    public Task<IParserService> FetchList();
    public List<NewsItem> GetNewsItems();
    public Task<string> GetParsePage(string url);
}