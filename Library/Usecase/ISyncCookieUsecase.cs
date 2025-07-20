using net_news_html.Models;

namespace net_news_html.Library.Usecase;

public interface ISyncCookieUsecase
{
    public Task SaveAllCookieNews(Guid PassKeyID);
}