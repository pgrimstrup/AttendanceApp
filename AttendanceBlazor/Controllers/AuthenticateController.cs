using System.Security.Claims;
using Attendance.Data;
using Attendance.Services;
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
    readonly IUserManager _users;

    public AuthenticateController(IOptions<AttendanceOptions> options, IUserManager userManager)
    {
        _options = options.Value;
        _users = userManager;
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
        SportyImport? user = null;

        if(!String.IsNullOrWhiteSpace(authCode))
        {
            if(Int32.TryParse(authCode.Replace(" ", ""), out var code))
            {
                if (!String.IsNullOrWhiteSpace(pnzNumber) && !String.IsNullOrWhiteSpace(authCode))
                {
                    user  = await _users.ValidateAccessCodeForPnzNumber(pnzNumber, code);
                }
                if (!String.IsNullOrEmpty(cardNumber))
                {
                    user = await _users.ValidateAccessCodeForCardNumber(cardNumber, code);
                }
            }
        }

        if (user != null)
        {
            List<Claim> claims = new()
            {
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Role, "Member"),
                new Claim("RFID", user.CardNumber ?? ""),
                new Claim(ClaimTypes.Email, user.EmailAddress ?? "")
            };

            if (!String.IsNullOrEmpty(user.FALNumber))
            {
                claims.Add(new Claim(ClaimTypes.Role, "FAL"));
                claims.Add(new Claim("FAL", user.FALNumber));
            }

            if (!String.IsNullOrEmpty(user.PNZNumber))
            {
                claims.Add(new Claim(ClaimTypes.Role, "PNZ"));
                claims.Add(new Claim("PNZ", user.PNZNumber));
            }

            if (!String.IsNullOrEmpty(user.CardNumber))
            {
                if (_options.Administrators.Contains(user.CardNumber) || user.CardNumber == "RRGC:ADMIN")
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

        return BadRequest("Invalid Access Code");
    }
}
