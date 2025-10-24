using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace AttendanceBlazor.HostedServices;

//public class CustomAuthenticationStateProvider : AuthenticationStateProvider
//{
//    readonly IHttpContextAccessor httpContextAccessor;

//    public CustomAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
//    {
//        this.httpContextAccessor = httpContextAccessor;
//    }

//    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
//    {
//        //// Example: Simulate a logged-in user with roles
//        //var identity = new ClaimsIdentity(new[]
//        //{
//        //    new Claim(ClaimTypes.Name, "Paul X"),
//        //    new Claim(ClaimTypes.Role, "Admin"),
//        //    new Claim(ClaimTypes.Role, "Member")
//        //}, "Cookies");

//        //var user = new ClaimsPrincipal(identity);

//        var httpContext = httpContextAccessor.HttpContext;
//        if (httpContext != null)
//            return new AuthenticationState(httpContext.User);

//        return new AuthenticationState(new ClaimsPrincipal());
//    }

//    public void NotifyAuthenticationStateChanged(ClaimsPrincipal user)
//    {
//        var authState = Task.FromResult(new AuthenticationState(user));
//        NotifyAuthenticationStateChanged(authState);
//    }
//}