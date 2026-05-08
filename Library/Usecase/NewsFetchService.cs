using System.Text.Json;
using net_news_html.Library.Interface;
using net_news_html.Library.Parser;
using net_news_html.Models;
using StackExchange.Redis;

namespace net_news_html.Library.Usecase;

public class NewsFetchService : INewsFetchService
{
    private static readonly (string Slug, string Title, string ParserType, string Url)[] Sources =
    [
        ("kontan-investasi",    "Kontan Investasi",       "Kontan",      "https://investasi.kontan.co.id"),
        ("kontan-fintech",      "Kontan Fintech",         "Kontan",      "https://www.kontan.co.id/search/?search=fintech"),
        ("kontan-berita",       "Kontan Berita",          "Kontan",      "https://nasional.kontan.co.id"),
        ("jagatreview-notebook","JagatReview Notebook",   "JagatReview", "https://www.jagatreview.com/category/mobile-computing/"),
        ("tempo-bisnis",        "Tempo Bisnis",           "Tempo",       "https://www.tempo.co/ekonomi/bisnis"),
    ];

    private readonly IServiceProvider _sp;
    private readonly IDatabase _redis;

    public NewsFetchService(IServiceProvider sp, ConnectionMultiplexer redis)
    {
        _sp = sp;
        _redis = redis.GetDatabase();
    }

    public async Task FetchAllAsync()
    {
        var tasks = Sources.Select(async s =>
        {
            _redis.KeyDelete($"feed:{s.Slug}:list");
            _redis.KeyDelete($"feed:{s.Slug}:date");

            var parser = s.ParserType switch
            {
                "Kontan"      => (IParserService)_sp.GetRequiredService<KontanParserService>(),
                "JagatReview" => _sp.GetRequiredService<JagatReviewParserService>(),
                _             => _sp.GetRequiredService<TempoParserService>(),
            };

            parser.SetListUrl(s.Url);
            await parser.FetchList();

            var items = parser.GetNewsItems();
            _redis.StringSet($"feed:{s.Slug}:list", JsonSerializer.Serialize(items), TimeSpan.FromMinutes(10));
            _redis.StringSet($"feed:{s.Slug}:date", DateTime.UtcNow.ToString("O"), TimeSpan.FromMinutes(10));
        });

        await Task.WhenAll(tasks);
    }

    public List<(string Slug, string Title, string Url)> GetSources() =>
        Sources.Select(s => (s.Slug, s.Title, s.Url)).ToList();
}
