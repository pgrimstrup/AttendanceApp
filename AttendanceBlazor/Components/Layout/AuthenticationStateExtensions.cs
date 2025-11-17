namespace AttendanceBlazor.Components.Layout;

using Microsoft.AspNetCore.Components.Authorization;

public static class AuthenticationStateExtensions
{
    extension(AuthenticationState context)
    {
        public bool IsInRole(string role)
        {
            var user = context.User;
            return user?.IsInRole(role) ?? false;
        }

        public string GetName()
        {
            var user = context.User;
            return user?.Identity?.Name ?? string.Empty;
        }

        public string GetPnzNumber()
        {
            var user = context.User;
            return user.Claims.FirstOrDefault(c => c.Type == "PNZ")?.Value ?? "";
        }
    }
}
