using System;

namespace net_news_html.Models
{
    public class SavedNewsItem : NewsItem
    {
        public DateTime SaveDate { get; set; }
        public Guid PassKeyId { get; set; }
        public PassKey PassKey { get; set; } = null!;
    }
}