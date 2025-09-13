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
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:142.0) Gecko/20100101 Firefox/142.0");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.tempo.co/");
        _httpClient.DefaultRequestHeaders.Add("X-Platform-Application", "desktop");

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
            // Build URL with parameters
            var baseUrl = _configuration["Tempo:BaseUrl"];
            var requestUrl = $"{baseUrl}?status=published&limit=25&page=1&page_size=25&order_published_at=DESC&rubric_slug={_rubrikSlug.ToLower()}";
            
            // Make GET request to Tempo API
            var response = await _httpClient.GetStringAsync(requestUrl);
            
            // Parse JSON response
            var jsonResponse = JsonDocument.Parse(response);
            
            // Transform articles into NewsItem objects
            _listNews.Clear();
            if (jsonResponse.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var article in data.EnumerateArray())
                {
                    if (article.TryGetProperty("title_digital", out var title) && 
                        article.TryGetProperty("canonical_url", out var canonicalUrl))
                    {
                        _listNews.Add(new NewsItem()
                        {
                            Title = title.GetString() ?? "",
                            Url = $"https://www.tempo.co/{canonicalUrl.GetString()}"
                        });
                    }
                }
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

            // Extract subtitle/description
            titleElement!.ParentElement!.QuerySelector("[data-v-19f156b9]")!.Remove();
            

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
            _rubrikSlug = url.Replace("https://", "").Split("/")[1];
        }
        catch (Exception e)
        {
            throw new Exception($"Broken Slug Rubric URL : {e.Message}");
        }
        
    }
}