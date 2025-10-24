using System.Globalization;
using System.Security.Claims;
using Attendance.Data;
using Attendance.Services;
using AttendanceBlazor.Components;
using AttendanceBlazor.HostedServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

#if DEBUG
var nz = CultureInfo.GetCultureInfo("en-NZ");
CultureInfo.DefaultThreadCurrentCulture = nz;
CultureInfo.DefaultThreadCurrentUICulture = nz;
// If you still use explicit thread cultures:
Thread.CurrentThread.CurrentCulture = nz;
Thread.CurrentThread.CurrentUICulture = nz;
#endif

var builder = WebApplication.CreateBuilder(args);

// Add authentication services
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie();

// Add authorization services
builder.Services.AddAuthorization(options => {
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MemberOnly", policy => policy.RequireRole("Member"));
});

builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddControllers(); // Using API controllers
builder.Services.AddRazorComponents() // Using interactive blazor pages
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IEntraPassImporter, EntraPassImporter>();
builder.Services.AddScoped<ISportyRegistrationImporter, SportyRegistrationImporter>();
builder.Services.AddScoped<ICalendarImporter, CalendarImporter>();

builder.Services.AddSingleton<ICalendarManager, CalendarManager>();
builder.Services.AddSingleton<IAttendanceManager, AttendanceManager>();

builder.Services.AddOptions<CalendarImporterOptions>()
    .Bind(builder.Configuration.GetSection(CalendarImporterOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<AttendanceOptions>()
    .Bind(builder.Configuration.GetSection(AttendanceOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<CalendarImportService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.MapControllers(); // Map API controllers

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated(); // Creates the database schema if it doesn't exist
    }
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//app.MapPost("/account/login", async (HttpContext http, string username, string? returnUrl) => {
//    // TODO: validate credentials. Simple demo user:
//    var claims = new[]
//    {
//        new Claim(ClaimTypes.Name, username),
//        new Claim(ClaimTypes.Role, "Admin"),
//        new Claim(ClaimTypes.Role, "Member"),
//    };
//    var id = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
//    await http.SignInAsync(
//        CookieAuthenticationDefaults.AuthenticationScheme,
//        new ClaimsPrincipal(id));

//    // round-trip back so the browser now carries the cookie
//    return Results.Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
//}).AllowAnonymous();

app.MapPost("/account/logout", async (HttpContext http) => {
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();
