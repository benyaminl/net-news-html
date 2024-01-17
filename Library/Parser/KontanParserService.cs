using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using net_news_html.Library.Interface;
using net_news_html.Models;

namespace net_news_html.Library.Parser;

public class KontanParserService : IParserService
{
    private HttpClient _http;
    public string listUrl = "";

    private List<NewsItem> _listNews = new List<NewsItem>();
    
    public KontanParserService(IHttpClientFactory _httpClientFactory)
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
                Url = url +"?page=all"
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
        var resp = await _http.GetStringAsync(url);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);
        
        document = ParserHelper.ParseWithProxy(document);
        document = ParserHelper.RemoveAllComment(document);

        var article = document.QuerySelector("*[itemprop=\"articleBody\"]");
        article.QuerySelector(".boxdonasi").Remove();
        var img = document.QuerySelector(".img-detail-desk").ToHtml();
        var title = document.QuerySelector(".detail-desk").ToHtml();
        
        foreach (var el in article.QuerySelectorAll(".track-bacajuga-inside, .pagination"))
        {
            el.ParentElement.Remove();
        }
        
        article.QuerySelector(".track-gnews").ParentElement.Remove();
        // article.QuerySelector("*[d-widget]").Remove();
        
        foreach (var el in article.QuerySelectorAll("script,link, iframe, *[d-widget], " +
                                    ".mar-v-10, .heightads250, h2, style, #share-it, .ads-partner-wrap, .kgnw-root"))
        {
            el.Remove();
        }
        
        // var obj = article.QuerySelector("#div-belowarticle-Investasi");
        // article.RemoveChild(obj.PreviousElementSibling.PreviousElementSibling);
        
        return  "<br/><br/>" + title + "<br/><br/>"+img + article.ToHtml();
    }
}