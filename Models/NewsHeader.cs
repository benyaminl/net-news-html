namespace net_news_html.Models;

public class NewsHeader
{
    public NewsHeader(string title, List<NewsItem> data, DateTime date)
    {
        LastUpdate = date;
        Title = title;
        News = data;
    }

    public NewsHeader(string title, List<NewsItem> data)
           : this(title, data, DateTime.Now)
    {
    }


    public NewsHeader(string title) 
        : this(title, new List<NewsItem>())
    {
    }

    public string Title { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<NewsItem> News { get; set; }
}