using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using net_news_html.Library.Interface;
using net_news_html.Models;
using StackExchange.Redis;

namespace net_news_html.Library.Parser;

public class JagatReviewParserService : IParserService
{
    private HttpClient _http;
    private string listUrl = "";

    private List<NewsItem> _listNews = new List<NewsItem>();
    private IDatabase _redis; 
    
    public JagatReviewParserService(IHttpClientFactory _httpClientFactory, ConnectionMultiplexer _redisFactory)
    {
        _http = _httpClientFactory.CreateClient("Firefox");
        _redis = _redisFactory.GetDatabase();
    }

    public void SetListUrl(string url)
    {
        this.listUrl = url;
    }

    public string GetListUrl()
    {
        return listUrl;
    }

    public async Task<IParserService> FetchList()
    {
        #region redis cache
        var data = _redis.StringGet(listUrl);
        if (!data.IsNull)
        {
            _listNews = JsonSerializer.Deserialize<List<NewsItem>>(data);
            return this;
        }
        #endregion

        try
        {
            var resp = await _http.GetStringAsync(listUrl);
            IHtmlParser parser = new HtmlParser();
            IDocument document = await parser.ParseDocumentAsync(resp);
            foreach (var el in document.QuerySelectorAll(".ct__main *[id*='article-'] a h2"))
            {
                var head = el.ParentElement;
                string title = el.Text();
                string url = ((IHtmlAnchorElement)head).Href;
                
                _listNews.Add(new NewsItem()
                {
                    Title = title,
                    Url = url
                });
            }
        }
        catch (Exception)
        {
            _listNews.Clear();
            return this;
        }        
        
        #region redis set data
        _redis.StringSet(listUrl, JsonSerializer.Serialize(_listNews), TimeSpan.FromMinutes(30));
        #endregion
        
        return this;
    }

    public List<NewsItem> GetNewsItems()
    {
        return _listNews;
    }

    public async Task<ParsedNews> GetParsePage(string url)
    {
        #region redis cache
        var data = _redis.StringGet(url);
        ParsedNews parsedNews;
        if (!data.IsNull)
        {
            parsedNews = JsonSerializer.Deserialize<ParsedNews>(data!)!;
            return parsedNews;
        }
        #endregion
        
        var resp = await _http.GetStringAsync(url);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);

        document = ParserHelper.ParseWithProxy(document);
        document = ParserHelper.RemoveAllComment(document);
        var title = document.QuerySelector(".jgpost__box h1")?.Text() ?? "";
        var article = document.QuerySelector(".jgpost__content")!;
        var pageCount = document.QuerySelectorAll(".toc-pages-list .post-page-numbers").Length;
        
        var tasks = new List<Task<IElement>>();
        for (int i = 2; i <= pageCount; i++)
        {
            int pageNumber = i; // Capture the loop variable
            tasks.Add(Task.Run(async () =>
            {
                var pageUrl = $"{url}/{pageNumber}";
                var anotherPage = await _http.GetStringAsync(pageUrl);
                IDocument documentArticle = await parser.ParseDocumentAsync(anotherPage);
        
                documentArticle = ParserHelper.ParseWithProxy(documentArticle);
                documentArticle = ParserHelper.RemoveAllComment(documentArticle);
                var articleSecondary = documentArticle.QuerySelector(".jgpost__content");
                documentArticle.QuerySelector(".jgauthor.breakout")?.Remove();
        
                return articleSecondary!;
            }));
        }
        
        var results = await Task.WhenAll(tasks);
        
        foreach (var articleSecondary in results)
        {
            if (articleSecondary != null)
            {
                article.AppendChild(articleSecondary);
            }
        }
        
        foreach (var el in article.QuerySelectorAll(".jgtoc"))
        {
            el.Remove();
        }

        foreach (var el in article.QuerySelectorAll(".jgtags"))
        {
            el.ParentElement!.Remove();
        }
        
        var body = "<style>img {max-width: 100%; height: auto !important;}</style>" + article.ToHtml() 
            +"<br><a href='"+url+"'>Source Berita</a>";

        parsedNews = new ParsedNews()
        {
            Title = title,
            Body = body,
            Url = url
        };
        
        #region redis set data
        _redis.StringSet(url, JsonSerializer.Serialize(parsedNews), TimeSpan.FromHours(72));
        #endregion
        
        return parsedNews;
    }

    public DateTime GetLastUpdate()
    {
        var newDate = DateTime.Now;
        #region redis cache
        var data = _redis.StringGet($"date:{listUrl}");
        if (!data.IsNull)
        {
            var dateTime = JsonSerializer.Deserialize<DateTime>(data);
            return dateTime;
        }
        else
        {
            _redis.StringSet($"date:{listUrl}", JsonSerializer.Serialize(newDate), TimeSpan.FromMinutes(30));
        }
        #endregion

        return newDate;
    }
}
