using System.Net;
using Microsoft.EntityFrameworkCore;
using net_news_html.Library.Interface;
using net_news_html.Library.Parser;
using net_news_html.Library.Storage;
using StackExchange.Redis;
using Microsoft.AspNetCore.DataProtection;
using net_news_html.Library.Usecase;
using Library.Interface;
using Library.Storage;

var builder = WebApplication.CreateBuilder(args);
var redisUrl = builder.Configuration.GetValue<string>("REDIS_URL");
Console.WriteLine("URL: " + redisUrl);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("Firefox", c =>
{
    c.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0");
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration.GetValue<string>("DataProtectionKeysPath")!))
    .SetApplicationName("net-news-html");

builder.Services.AddTransient<KontanParserService>();
builder.Services.AddTransient<JagatReviewParserService>();
builder.Services.AddTransient<TempoParserService>();
builder.Services.AddSingleton<ConnectionMultiplexer>(_ =>
{
    var configuration = ConfigurationOptions.Parse(redisUrl!);
    configuration.ConnectRetry = 3;
    configuration.ReconnectRetryPolicy = new ExponentialRetry(5000);
    configuration.KeepAlive = 180;
    configuration.ConnectTimeout = 30000;
    configuration.SyncTimeout = 30000;
    configuration.AbortOnConnectFail = false;
    
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddDbContext<NewsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("SqliteFile")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<INewsStorage, NewsStorageCookieImpl>();
builder.Services.AddScoped<INewsPersistenceStrorage, NewsStorageSqliteImpl>();
builder.Services.AddScoped<ISyncCookieUsecase, SyncCookieDBUsecase>();
builder.Services.AddScoped<IPassStorage, PassStorageSqliteImpl>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<NewsDbContext>();
        context.Database.Migrate(); // This applies pending migrations instead of just creating the DB
        Console.WriteLine("Database path: " + context.Database.GetConnectionString());
    }
    catch (Exception ex)
    {
        Console.WriteLine("An error occurred with the DB: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
