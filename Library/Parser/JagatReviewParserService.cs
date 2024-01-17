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

        var article = document.QuerySelector(".jgpost__content");
        var pageCount = document.QuerySelectorAll(".toc-pages-list .post-page-numbers").Length;

        for (int i = 1; i < pageCount; i++)
        {
            var anotherPage = await _http.GetStringAsync(url+"/"+(i+1));
            var documentArticle = await parser.ParseDocumentAsync(anotherPage);

            var articleSecondary = documentArticle.QuerySelector(".jgpost__content");
            documentArticle.QuerySelector(".jgauthor.breakout").Remove();

            article.AppendChild(articleSecondary);
        }
        
        foreach (var el in article.QuerySelectorAll(".jgtoc"))
        {
            el.Remove();
        }

        foreach (var el in article.QuerySelectorAll(".jgtags"))
        {
            el.ParentElement.Remove();
        }
        
        return "<style>img {max-width: 100%; height: auto !important;}</style>" + article.ToHtml();
    }
}