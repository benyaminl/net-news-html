using System.Net;
using System.Text.Json;
using net_news_html.Library.Interface;
using net_news_html.Models;
using StackExchange.Redis;
using AngleSharp.Html.Parser;
using AngleSharp.Dom;

namespace net_news_html.Library.Parser;

public class TempoParserService : IParserService
{
    private readonly HttpClient _httpClient;
    private readonly IDatabase _redis;
    private readonly IConfiguration _configuration;
    private List<NewsItem> _listNews = new List<NewsItem>();

    private string _rubrikSlug = "";

    public TempoParserService(IHttpClientFactory httpClientFactory, ConnectionMultiplexer redisFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        
        // Use the named client configured with decompression support
        _httpClient = httpClientFactory.CreateClient("Firefox");
        
        // Configure automatic decompression
        if (_httpClient.DefaultRequestHeaders.Contains("Accept-Encoding"))
        {
            _httpClient.DefaultRequestHeaders.Remove("Accept-Encoding");
        }
        
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.tempo.co/indeks?category=rubrik&id=9&rubric_slug=ekonomi&page=1");
        _httpClient.DefaultRequestHeaders.Add("X-Platform-Application", "desktop");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");

        _redis = redisFactory.GetDatabase();
    }

    public async Task<IParserService> FetchList()
    {
        var cacheKey = $"tempo:list:{_rubrikSlug}";
        
        // Check Redis cache first
        var cachedData = _redis.StringGet(cacheKey);
        if (!cachedData.IsNull)
        {
            _listNews = JsonSerializer.Deserialize<List<NewsItem>>(cachedData!)!;
            return this;
        }
        
        try
        {
            // Build URL for the rubrik page
            var baseUrl = _configuration["Tempo:BaseUrl"];
            var requestUrl = $"{baseUrl}/{_rubrikSlug}";
            
            // Fetch HTML page
            var response = await _httpClient.GetStringAsync(requestUrl);
            
            // Parse HTML with AngleSharp
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(response);
            
            // Extract article links
            _listNews.Clear();
            var links = document.QuerySelectorAll("a[href^='/ekonomi/']");
            
            foreach (var link in links)
            {
                var href = link.GetAttribute("href");
                var title = link.TextContent.Trim();
                
                // Skip empty titles or non-article links
                if (string.IsNullOrWhiteSpace(title) || !href.Contains("-"))
                    continue;
                
                _listNews.Add(new NewsItem()
                {
                    Title = title,
                    Url = $"https://www.tempo.co{href}"
                });
            }
            
            // Store parsed list in Redis cache with 30-minute expiration
            _redis.StringSet(cacheKey, JsonSerializer.Serialize(_listNews), TimeSpan.FromMinutes(30));
        }
        catch (Exception)
        {
            _listNews.Clear();
        }
        
        return this;
    }

    public DateTime GetLastUpdate()
    {
        var newDate = DateTime.Now;
        var cacheKey = $"tempo:date:{_rubrikSlug}";
        
        // Check Redis cache
        var cachedData = _redis.StringGet(cacheKey);
        if (!cachedData.IsNull)
        {
            var dateTime = JsonSerializer.Deserialize<DateTime>(cachedData!);
            return dateTime;
        }
        else
        {
            _redis.StringSet(cacheKey, JsonSerializer.Serialize(newDate), TimeSpan.FromMinutes(30));
        }
        
        return newDate;
    }

    public string GetListUrl()
    {
        return _configuration["Tempo:BaseUrl"] + $"/{_rubrikSlug}";
    }

    public List<NewsItem> GetNewsItems()
    {
        return _listNews;
    }

    public async Task<ParsedNews> GetParsePage(string url)
    {
        var cacheKey = $"tempo:page:{url}";
        
        // Check Redis cache first
        var cachedData = _redis.StringGet(cacheKey);
        if (!cachedData.IsNull)
        {
            var parsedNews = JsonSerializer.Deserialize<ParsedNews>(cachedData!)!;
            return parsedNews;
        }
        
        try
        {
            // Fetch article content from URL
            var response = await _httpClient.GetStringAsync(url);

            // Parse HTML content with AngleSharp
            var parser = new HtmlParser();
            IDocument document = await parser.ParseDocumentAsync(response);

            document = ParserHelper.ParseWithProxy(document);
            document = ParserHelper.RemoveAllComment(document);
            
            // Extract title
            var titleElement = document.QuerySelector("h1");
            var title = titleElement?.Text() ?? "";

            // Remove first item bisnis
            titleElement!.ParentElement!.FirstChild!.RemoveFromParent();
            

            var subtitleParent = titleElement!.ParentElement!;
            var subtitleElement = subtitleParent;
            titleElement.Remove();
            var subtitle = subtitleElement?.Html() ?? "";
            
        
            // Extract article content
            var articleElement = document.QuerySelector("article"); // Assuming the main content is in an <article> tag

            // Clean up 
            var ads = articleElement!.QuerySelectorAll("*[ad-unit-path]");
            foreach (var ad in ads)
            {
                ad!.ParentElement!.Remove();
            }

            articleElement!.QuerySelector("#remp-detail-baca-juga-1")!.ParentElement!.ParentElement!.Remove();
            articleElement!.QuerySelector("#article-tags")!.Remove();
            articleElement!.QuerySelector("#feature_image")!.NextElementSibling!.Remove();

            var articleContent = articleElement?.Html() ?? "";
            
            // Combine title, subtitle, and article content
            var body = $"<h1>{title}</h1><p><strong>{subtitle}</strong></p><br/>{articleContent}<br><a href='{url}'>Source Berita</a>";
            
            var parsedNews = new ParsedNews()
            {
                Title = title,
                Body = body,
                Url = url
            };
            
            // Store parsed result in Redis cache with 6-hour expiration
            _redis.StringSet(cacheKey, JsonSerializer.Serialize(parsedNews), TimeSpan.FromHours(6));
            
            return parsedNews;
        }
        catch (Exception)
        {
            // Return a default ParsedNews object in case of error
            return new ParsedNews()
            {
                Title = "",
                Body = "",
                Url = url
            };
        }
    }

    public void SetListUrl(string url)
    {
        try
        {
            // Extract the path after the domain
            var urlParts = url.Replace("https://", "").Replace("http://", "").Split("/");
            
            // Handle URLs like "ekonomi/bisnis" or just "ekonomi"
            if (urlParts.Length >= 3)
            {
                // URL format: tempo.co/ekonomi/bisnis
                _rubrikSlug = $"{urlParts[1]}/{urlParts[2]}";
            }
            else if (urlParts.Length == 2)
            {
                // URL format: tempo.co/ekonomi
                _rubrikSlug = urlParts[1];
            }
            else
            {
                throw new Exception("Invalid URL format");
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Broken Slug Rubric URL : {e.Message}");
        }
        
    }
}