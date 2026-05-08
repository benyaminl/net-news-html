using System.Diagnostics;
using System.Text.Json;
using System.Web;
using Library.Interface;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using net_news_html.Library.Interface;
using net_news_html.Library.Parser;
using net_news_html.Models;
using net_news_html.Library.ViewModel;
using StackExchange.Redis;

namespace net_news_html.Controllers;

public class HomeController(ILogger<HomeController> logger, IServiceProvider service, IHttpClientFactory clientFactory, INewsStorage newsStorage, IDataProtectionProvider dataProtectionProvider, IPassStorage passStorage, INewsFetchService fetchService, ConnectionMultiplexer redis) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly IDatabase _redis = redis.GetDatabase();

    public async Task<IActionResult> Index()
    {
        var sources = fetchService.GetSources();
        var data = new List<NewsHeader>();

        foreach (var (slug, title, url) in sources)
        {
            var listRaw = await _redis.StringGetAsync($"feed:{slug}:list");
            var dateRaw = await _redis.StringGetAsync($"feed:{slug}:date");

            var items = listRaw.IsNull
                ? new List<NewsItem>()
                : JsonSerializer.Deserialize<List<NewsItem>>(listRaw!) ?? new List<NewsItem>();

            var date = dateRaw.IsNull ? DateTime.UtcNow : DateTime.Parse(dateRaw!);

            data.Add(new NewsHeader(title: title, data: items, date: date) { Slug = slug });
        }

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
            service!.GetService<TempoParserService>()!,
        ];

        string[] homeUrls =
        [
            "https://investasi.kontan.co.id",
            "https://keuangan.kontan.co.id",
            "https://nasional.kontan.co.id",
            "https://www.jagatreview.com",
            "https://www.tempo.co"
        ];

        ParsedNews? result = null;

        for (int i = 0; i < homeUrls.Length; i++)
        {
            if (url.Contains(homeUrls[i]))
                result = await parserServices[i].GetParsePage(url);
        }

        result ??= new ParsedNews
        {
            Title = "Not Found",
            Body = $"There are something wrong, go to <a href='{url}'>Source Page</a> directly."
        };

        return View(model: new { body = result.Body, data = result });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private bool userLoggedIn = false;

    public override async void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.HttpContext.Request.Cookies["PassKeyId"] != null)
        {
            newsStorage = service!.GetService<INewsPersistenceStrorage>()!;
            userLoggedIn = true;
            var protector = dataProtectionProvider.CreateProtector("PassKeyAuth");
            var passKey = protector.Unprotect(Request.Cookies["PassKeyId"]!);
            var passKeyData = await passStorage.GetPassKey(passKey);
            if (passKeyData != null)
                owner = passKeyData.Id;
        }
        else
        {
            newsStorage = service!.GetService<INewsStorage>()!;
        }
        base.OnActionExecuting(context);
    }

    [HttpGet("/save")]
    public async Task<IActionResult> SaveNews([FromQuery] string url, [FromQuery] string title)
    {
        await newsStorage.SaveNewsAsync(new SavedNewsItem
        {
            PassKeyId = owner ?? Guid.Empty,
            Url = url,
            Title = title,
            SaveDate = DateTime.Now
        });
        return Redirect("~/saved");
    }

    private Guid? owner = null;

    [HttpGet("/saved")]
    public async Task<IActionResult> Saved()
    {
        var data = await newsStorage.GetNewsAsync(owner);
        var viewModel = new SavedNewsViewModel()
        {
            listSavedNews = data,
            loggedIn = userLoggedIn
        };
        return View("~/Views/Home/ViewSavedNews.cshtml", viewModel);
    }

    [HttpGet("/remove-saved")]
    public async Task<IActionResult> RemoveSaved([FromQuery] string url)
    {
        await newsStorage.DeleteNewsAsync(url);
        return Redirect("~/saved");
    }
}
