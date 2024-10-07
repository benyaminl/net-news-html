using System.Text.Json;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using net_news_html.Library.Interface;
using net_news_html.Models;
using StackExchange.Redis;

namespace net_news_html.Library.Parser;

public class KontanParserService : IParserService
{
    private HttpClient _http;
    public string listUrl = "";
    private IDatabase _redis;

    private List<NewsItem> _listNews = new List<NewsItem>();
    
    public KontanParserService(IHttpClientFactory _httpClientFactory, ConnectionMultiplexer _redisFactory)
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
        
        var resp = await _http.GetStringAsync(listUrl);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);
        
        foreach (var el in document.QuerySelectorAll("#list-news li, .list-berita li"))
        {
            var head = el.QuerySelector<IHtmlAnchorElement>("h1 a");
            try {
                string title = head!.Text();
                string url = head!.Href.Replace("about:","https:");
                
                if (!url.Contains("insight.kontan"))
                {
                    _listNews.Add(new NewsItem()
                    {
                        Title = title,
                        Url = url +"?page=all"
                    });
                }
            }
            catch(Exception e) {
                
            }
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

    public async Task<string> GetParsePage(string url)
    {
        #region redis cache
        var data = _redis.StringGet(url);
        if (!data.IsNull)
        {
            return data;
        }
        #endregion
        
        var resp = await _http.GetStringAsync(url);
        IHtmlParser parser = new HtmlParser();
        IDocument document = await parser.ParseDocumentAsync(resp);
        
        document = ParserHelper.ParseWithProxy(document);
        document = ParserHelper.RemoveAllComment(document);

        var article = document.QuerySelector("*[itemprop=\"articleBody\"]");
        #region remove useless things
        article!.QuerySelector(".boxdonasi")?.Remove();
        article.QuerySelector(".track-lanjutbaca")?.Remove();
        #endregion
        var img = document.QuerySelector(".img-detail-desk")!.ToHtml();
        var title = document.QuerySelector(".detail-desk")!.ToHtml();
        
        foreach (var el in article.QuerySelectorAll(".track-bacajuga-inside, .pagination"))
        {
            el.ParentElement!.Remove();
        }
        
        article.QuerySelector(".track-gnews")!.ParentElement.Remove();
        // article.QuerySelector("*[d-widget]").Remove();
        
        foreach (var el in article.QuerySelectorAll("script,link, iframe, *[d-widget], " +
                                    ".mar-v-10, .heightads250, h2, style, #share-it, .ads-partner-wrap, .kgnw-root"))
        {
            el.Remove();
        }
        
        // var obj = article.QuerySelector("#div-belowarticle-Investasi");
        // article.RemoveChild(obj.PreviousElementSibling.PreviousElementSibling);
        
        var returnVal =  "<br/><br/>" + title + "<br/><br/>"+img + article.ToHtml()+"<br><a href='"+url+"'>Source Berita</a>";
        
        #region redis set data
        _redis.StringSet(url, returnVal, TimeSpan.FromHours(6));
        #endregion
        
        return returnVal;
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