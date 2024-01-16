using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using net_news_html.Library.Interface;
using net_news_html.Models;

namespace net_news_html.Library.Parser;

public class JagatReviewParserService : IParserService
{
    private HttpClient _http;
    private string listUrl = "";

    private List<NewsItem> _listNews = new List<NewsItem>();
    
    public JagatReviewParserService(IHttpClientFactory _httpClientFactory)
    {
        _http = _httpClientFactory.CreateClient("Firefox");
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
        var resp = await _http.GetStringAsync(listUrl);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);
        foreach (var el in document.QuerySelectorAll("#list-news li"))
        {
            var head = el.QuerySelector<IHtmlAnchorElement>("h1 a");
            string title = head.Text();
            string url = head.Href;
            
            _listNews.Add(new NewsItem()
            {
                Title = title,
                Url = url
            });
        }
        
        return this;
    }

    public List<NewsItem> GetNewsItems()
    {
        return _listNews;
    }

    public async Task<string> GetParsePage(string url)
    {
        var resp = await _http.GetStringAsync(listUrl);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);

        foreach (var el in document.QuerySelectorAll(""))
        {
            el.Remove();
        }

        document = ParserHelper.ParseWithProxy(document);

        return document.ToHtml();
    }
}