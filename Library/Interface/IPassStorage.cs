using net_news_html.Models;

namespace Library.Interface;

public interface IPassStorage
{
    /// <summary>
    ///     Saves the passkey.
    /// </summary>
    /// <param name="key">The key.</param>
    Task<PassKey?> SavePasskey(string key);

    bool IsPassKeyExists(string key);

    Task UpdatePasskeyLastUsedAt(string key);

    Task<PassKey?> GetPassKey(string key);
}