using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Library.Interface;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using net_news_html.Library.Interface;
using net_news_html.Library.Parser;
using net_news_html.Models;
using net_news_html.Library.ViewModel;

namespace net_news_html.Controllers;

public class HomeController(ILogger<HomeController> logger, IServiceProvider service, IHttpClientFactory clientFactory, INewsStorage newsStorage, IDataProtectionProvider dataProtectionProvider, IPassStorage passStorage) : Controller
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
            service!.GetService<TempoParserService>()!,
        };

        var titles = new[] { "Kontan Investasi", "Kontan Fintech", "Kontan Berita", "JagatReview Notebook", "Tempo Ekonomi" };
        var homeUrls = new[]
        {
            "https://investasi.kontan.co.id",
            "https://www.kontan.co.id/search/?search=fintech",
            "https://nasional.kontan.co.id",
            "https://www.jagatreview.com/category/mobile-computing/",
            "https://www.tempo.co/ekonomi"
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
            service!.GetService<TempoParserService>()!,
        ];
        
        string[] homeUrls = [
            "https://investasi.kontan.co.id",
            "https://keuangan.kontan.co.id",
            "https://nasional.kontan.co.id",
            "https://www.jagatreview.com",
            "https://www.tempo.co" 
        ];

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
            {
                owner = passKeyData.Id;
            }
        
        }
        else
        {
            newsStorage = service!.GetService<INewsStorage>()!;
        }
        base.OnActionExecuting(context);
    }

    // function to get data from query param with url, title then save to cookie with name "news"
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
    // function that return data from cookie with name "news"
    [HttpGet("/saved")]
    public async Task<IActionResult> Saved()
    {
        // TODO: Implement check user logged in or not 

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
