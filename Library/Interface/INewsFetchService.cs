namespace net_news_html.Library.Interface;

public interface INewsFetchService
{
    Task FetchAllAsync();
    List<(string Slug, string Title, string Url)> GetSources();
}
