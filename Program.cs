using Microsoft.AspNetCore.CookiePolicy;
using net_news_html.Library.Parser;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var redisUrl = builder.Configuration.GetValue<string>("REDIS_URL");
Console.WriteLine("URL: " + redisUrl);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("Firefox", c =>
    c.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0"));
builder.Services.AddTransient<KontanParserService>();
builder.Services.AddTransient<JagatReviewParserService>();
builder.Services.AddSingleton(x => ConnectionMultiplexer
    .Connect(redisUrl!));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None;
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
