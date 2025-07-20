using net_news_html.Library.Interface;
using net_news_html.Models;

namespace net_news_html.Library.Usecase;

public class SyncCookieDBUsecase
(INewsPersistenceStrorage newsPersistenceStrorage,
 INewsStorage cookieNewsStorage) : ISyncCookieUsecase
{

    public async Task SaveAllCookieNews(Guid PassKeyID)
    {
        var data = await cookieNewsStorage.GetNewsAsync(null);

        data.ForEach(async d =>
        {
            var savedNews = new SavedNewsItem()
            {
                PassKeyId = PassKeyID,
                SaveDate = DateTime.Now,
                Title = d.Title,
                Url = d.Url
            };

            await newsPersistenceStrorage.SaveNewsAsync(savedNews);
        });

    }

}