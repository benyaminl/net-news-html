using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using net_news_html.Library.Interface;
using net_news_html.Models;
using StackExchange.Redis;

namespace net_news_html.Controllers;

[Route("rss")]
public class RssController(INewsFetchService fetchService, ConnectionMultiplexer redis) : Controller
{
    private readonly IDatabase _redis = redis.GetDatabase();
    private const string Rfc822 = "ddd, dd MMM yyyy HH:mm:ss +0000";

    [HttpGet("{slug}")]
    public async Task<IActionResult> Feed([FromRoute] string slug)
    {
        var source = fetchService.GetSources().FirstOrDefault(s => s.Slug == slug);
        if (source == default)
            return NotFound();

        var (items, date) = await ReadFeed(slug);
        var xml = BuildRss(source.Title, source.Url, date, items.Select(i => (i, source.Title)));
        return Content(xml, "application/rss+xml; charset=utf-8");
    }

    [HttpGet("")]
    public async Task<IActionResult> Combined()
    {
        var sources = fetchService.GetSources();
        var allItems = new List<(NewsItem item, string category)>();
        var latestDate = DateTime.MinValue;

        foreach (var (slug, title, url) in sources)
        {
            var (items, date) = await ReadFeed(slug);
            allItems.AddRange(items.Select(i => (i, title)));
            if (date > latestDate) latestDate = date;
        }

        if (latestDate == DateTime.MinValue) latestDate = DateTime.UtcNow;

        var xml = BuildRss("All News", "/rss", latestDate, allItems);
        return Content(xml, "application/rss+xml; charset=utf-8");
    }

    private async Task<(List<NewsItem> items, DateTime date)> ReadFeed(string slug)
    {
        var listRaw = await _redis.StringGetAsync($"feed:{slug}:list");
        var dateRaw = await _redis.StringGetAsync($"feed:{slug}:date");

        var items = listRaw.IsNull
            ? new List<NewsItem>()
            : JsonSerializer.Deserialize<List<NewsItem>>(listRaw!) ?? new List<NewsItem>();

        var date = dateRaw.IsNull ? DateTime.UtcNow : DateTime.Parse(dateRaw!);

        return (items, date);
    }

    private static string BuildRss(string title, string link, DateTime date, IEnumerable<(NewsItem item, string category)> entries)
    {
        var channel = new XElement("channel",
            new XElement("title", title),
            new XElement("link", link),
            new XElement("lastBuildDate", date.ToString(Rfc822))
        );

        foreach (var (item, category) in entries)
        {
            channel.Add(new XElement("item",
                new XElement("title", item.Title),
                new XElement("link", item.Url),
                new XElement("category", category)
            ));
        }

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss", new XAttribute("version", "2.0"), channel)
        );

        return doc.Declaration + doc.ToString();
    }
}
