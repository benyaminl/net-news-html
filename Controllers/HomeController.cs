using System.Diagnostics;
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
        List<IParserService> parserServices =
        [
            service.GetService<KontanParserService>(),
            service.GetService<JagatReviewParserService>(),
            service.GetService<KontanParserService>()
        ];

        string[] titles = ["Kontan Investasi", "JagatReview Notebook", "Kontan Berita"];
        string[] homeUrls = [
            "https://investasi.kontan.co.id", 
            "https://www.jagatreview.com/category/mobile-computing/", 
            "https://nasional.kontan.co.id"];
        
        List<NewsHeader> data = [];
        
        for (var i = 0; i < parserServices.Count; i++)
        {
            var ps = parserServices[i];
            ps.SetListUrl(homeUrls[i]);
            
            var list = (await ps.FetchList()).GetNewsItems();
            var header = new NewsHeader()
            {
                Title = titles[i],
                News = list
            };
            
            data.Add(header);
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
        url = HttpUtility.UrlDecode(url);
        
        List<IParserService> parserServices =
        [
            service.GetService<KontanParserService>(),
            service.GetService<JagatReviewParserService>(),
            service.GetService<KontanParserService>()
        ];
        
        string[] homeUrls = [
            "https://investasi.kontan.co.id", 
            "https://www.jagatreview.com", 
            "https://nasional.kontan.co.id"];
        string result = "";

        for (int i = 0; i < homeUrls.Length; i++)
        {
            var homeUrl = homeUrls[i];

            if (url.Contains(homeUrl))
            {
                result = await parserServices[i].GetParsePage(url);
            }
        }
        
        return View(model: result);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
