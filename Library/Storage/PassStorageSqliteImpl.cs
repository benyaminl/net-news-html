using System.Threading.Tasks;
using Library.Interface;
using Microsoft.EntityFrameworkCore;
using net_news_html.Library.Storage;
using net_news_html.Models;

namespace Library.Storage;

public class PassStorageSqliteImpl(
    NewsDbContext _dbContext
) : IPassStorage
{
    public async Task<PassKey?> GetPassKey(string key)
    {
        var passKey = await _dbContext.PassKeys.FirstOrDefaultAsync(pk => pk.Key == key);

        return passKey;
    }

    public bool IsPassKeyExists(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var passKey = _dbContext.PassKeys.FirstOrDefault(pk => pk.Key == key);
        return passKey != null;
    }

    public async Task<PassKey?> SavePasskey(string key)
    {
        var passKey = await _dbContext.PassKeys.AddAsync(new PassKey
        {
            Key = key,
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = null
        });

        return passKey?.Entity;
    }

    public async Task UpdatePasskeyLastUsedAt(string key)
    {
        await _dbContext.PassKeys
            .Where(pk => pk.Key == key)
            .ExecuteUpdateAsync(setters => setters.SetProperty(pk => pk.LastUsedAt, DateTime.UtcNow));
    }
}