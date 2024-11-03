namespace net_news_html.Models;

// class with property Title, Url, Body all strings
public class ParsedNews
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string Body { get; set; } = null!;
}