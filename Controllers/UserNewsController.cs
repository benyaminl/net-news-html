using Library.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.Tasks;
using net_news_html.Library.Usecase;
using net_news_html.Library.Interface;

namespace net_news_html.Controllers;

public class UserNewsController
(IPassStorage passStorage, IDataProtectionProvider dataProtectionProvider,
 ISyncCookieUsecase syncCookieUsecase) 
: Controller
{

    public IActionResult Logout()
    {
        Response.Cookies.Delete("PassKeyId");
        return Ok("Logged out successfully.");
    }

    [HttpPost]
    public async Task<IActionResult> LoadWithPassKey([FromForm] string passcode)
    {
        if (string.IsNullOrEmpty(passcode))
        {
            return BadRequest("Key cannot be null or empty.");
        }

        var result = passStorage.IsPassKeyExists(passcode);

        if (result)
        {
            var savedPassKey = await passStorage.GetPassKey(passcode);
            await passStorage.UpdatePasskeyLastUsedAt(passcode);

            var protector = dataProtectionProvider.CreateProtector("PassKeyAuth");
            var encryptedId = protector.Protect(passcode);

            Response.Cookies.Append("PassKeyId", encryptedId, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            await syncCookieUsecase.SaveAllCookieNews(savedPassKey!.Id);

            return Ok("Passkey authenticated successfully.");
        }
        else
        {
            return Unauthorized("Invalid passkey.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrSaveWithPassKey([FromForm] string passcode)
    {
        if (string.IsNullOrEmpty(passcode))
        {
            return BadRequest("Key cannot be null or empty.");
        }

        var result = passStorage.IsPassKeyExists(passcode);

        if (result)
        {
            return BadRequest("Passkey exists already.");
        }

        var savedPassKey = await passStorage.SavePasskey(passcode);

        if (savedPassKey == null)
            return StatusCode(500, "Failed to create new passkey");

        var protector = dataProtectionProvider.CreateProtector("PassKeyAuth");
        var encryptedId = protector.Protect(passcode);

        Response.Cookies.Append("PassKeyId", encryptedId, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });


        await syncCookieUsecase.SaveAllCookieNews(savedPassKey!.Id);

        return Ok("Passkey created successfully.");
    }
}