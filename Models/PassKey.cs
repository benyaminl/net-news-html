using Microsoft.EntityFrameworkCore;

namespace net_news_html.Models;

[PrimaryKey("Id")]
public class PassKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; } = null;
}