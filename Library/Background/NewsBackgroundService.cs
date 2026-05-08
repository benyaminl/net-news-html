using net_news_html.Library.Interface;

namespace net_news_html.Library.Background;

public class NewsBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NewsBackgroundService> _logger;

    public NewsBackgroundService(IServiceScopeFactory scopeFactory, ILogger<NewsBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("NewsBackgroundService: Fetching all sources...");
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<INewsFetchService>();
                await svc.FetchAllAsync();
                _logger.LogInformation("NewsBackgroundService: Fetch complete.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NewsBackgroundService: Fetch failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
