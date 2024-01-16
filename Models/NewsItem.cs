using System.Web;

namespace net_news_html.Models;

public class NewsItem
{
    public string Title { get; set; }
    public string Url { get; set; }
    
    public string ProxyUrl => "/news/" + HttpUtility.UrlEncode(Url);
}