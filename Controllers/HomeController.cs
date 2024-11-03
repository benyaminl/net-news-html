using System.Diagnostics;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using net_news_html.Library.Interface;
using net_news_html.Library.Parser;
using net_news_html.Models;

namespace net_news_html.Controllers;

public class HomeController(ILogger<HomeController> logger, IServiceProvider service, IHttpClientFactory clientFactory) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;


    public async Task<IActionResult> Index()
    {
        var parserServices = new List<IParserService>
        {
            service!.GetService<KontanParserService>()!,
            service!.GetService<KontanParserService>()!,
            service!.GetService<KontanParserService>()!,
            service!.GetService<JagatReviewParserService>()!,
        };

        var titles = new[] { "Kontan Investasi", "Kontan Fintech", "Kontan Berita", "JagatReview Notebook" };
        var homeUrls = new[]
        {
            "https://investasi.kontan.co.id",
            "https://www.kontan.co.id/search/?search=fintech",
            "https://nasional.kontan.co.id",
            "https://www.jagatreview.com/category/mobile-computing/"
        };

        var tasks = parserServices.Select((ps, i) =>
        {
            ps.SetListUrl(homeUrls[i]);
            return ps.FetchList();
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        var data = Enumerable.Range(0, parserServices.Count)
            .Select(i => new NewsHeader(
                title: titles[i],
                data: results[i].GetNewsItems(),
                date: results[i].GetLastUpdate()
            ))
            .ToList();

        return View(data);
    }

    [HttpGet("/Proxy/{url}")]
    public async Task<IActionResult> Proxy([FromRoute] string url)
    {
        url = HttpUtility.UrlDecode(url);
        
        var client = clientFactory.CreateClient();
        var result = await client.GetStreamAsync(url);
        
        return Ok(result);
    }    
    
    [HttpGet("/news/{url}")]
    public async Task<IActionResult> News([FromRoute] string url)
    {
        Console.WriteLine(url);
        url = HttpUtility.UrlDecode(url);
        
        List<IParserService> parserServices =
        [
            service!.GetService<KontanParserService>()!,
            service!.GetService<KontanParserService>()!,            
            service!.GetService<KontanParserService>()!,
            service!.GetService<JagatReviewParserService>()!,
        ];
        
        string[] homeUrls = [
            "https://investasi.kontan.co.id",
            "https://keuangan.kontan.co.id",             
            "https://nasional.kontan.co.id",
            "https://www.jagatreview.com" ];

        ParsedNews? result = null;

        for (int i = 0; i < homeUrls.Length; i++)
        {
            var homeUrl = homeUrls[i];

            if (url.Contains(homeUrl))
            {
                result = await parserServices[i].GetParsePage(url);
            }
        }

        if (result == null)
        {
            result = new ParsedNews { 
                Title = "Not Found", 
                Body = $"There are something wrong, go to <a href='{url}'>Source Page</a> directly."
            };
        }
        
        return View(model: new {body = result.Body, data = result});
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // function to get data from query param with url, title then save to cookie with name "news"
    [HttpGet("/save")]
    public IActionResult SaveNews([FromQuery] string url, [FromQuery] string title)
    {
        var cookie = Request.Cookies["news"] ?? "";
        List<SavedNewsItem> data = new List<SavedNewsItem>();
        if (cookie != "")
        {
            data = JsonSerializer.Deserialize<List<SavedNewsItem>>(cookie)!;
        }
        
        data.Add(new SavedNewsItem
        {
            Url = url,
            Title = title,
            SaveDate = DateTime.Now 
        });

        cookie = JsonSerializer.Serialize(data);

        Response.Cookies.Append("news", cookie, new CookieOptions
        {
            Expires = DateTime.Now.AddDays(300),
            SameSite = SameSiteMode.None,
            Secure = true
        });

        return Redirect("~/saved");
    }

    // function that return data from cookie with name "news"
    [HttpGet("/saved")]
    public IActionResult Saved()
    {
        var cookie = Request.Cookies["news"] ?? "";
        List<SavedNewsItem> data = new List<SavedNewsItem>();
        if (cookie != "")
        {
            data = JsonSerializer.Deserialize<List<SavedNewsItem>>(cookie)!;
        }

        return View("~/Views/Home/ViewSavedNews.cshtml", data);
    }
}
