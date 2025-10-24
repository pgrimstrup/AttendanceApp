using System.Security.Claims;
using Attendance.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AttendanceBlazor.Controllers;

[Route("api/authenticate")]
[ApiController]
public class AuthenticateController : ControllerBase
{
    readonly AttendanceOptions _options;

    public AuthenticateController(IOptions<AttendanceOptions> options)
    {
        _options = options.Value;
    }


    [HttpPost]
    public async Task<IActionResult> Post(
        [FromForm(Name = "Nonce")] string? nonce,
        [FromForm(Name = "returnUrl")] string? returnUrl,
        [FromForm(Name = "PNZNumber")] string? pnzNumber,
        [FromForm(Name = "CardNumber")] string? cardNumber,
        [FromForm(Name = "AuthCode")] string? authCode,
        [FromForm(Name = "RememberMe")] bool? rememberMe)
    {
        // TODO: validate credentials. Simple demo user:
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Name, "Paul G"),
            //new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Member"),
        };

        if (!String.IsNullOrEmpty(pnzNumber))
        {
            claims.Add(new Claim(ClaimTypes.Role, "PNZ"));
            claims.Add(new Claim(ClaimTypes.Role, "PNZ:" + pnzNumber));
        }
        if (!String.IsNullOrEmpty(cardNumber))
        { 
            if(_options.Administrators.Contains(cardNumber))
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
        }

        var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var properties = new AuthenticationProperties {
            IsPersistent = rememberMe == true,
            ExpiresUtc = rememberMe == true ? DateTimeOffset.UtcNow.AddDays(30) : null
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(id),
            properties);

        return Redirect(String.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
    }
}
