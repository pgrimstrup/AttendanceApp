using System.Globalization;
using Attendance.Data;
using Attendance.Services;
using AttendanceBlazor.Components;
using AttendanceBlazor.HostedServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

#if DEBUG
var nz = CultureInfo.GetCultureInfo("en-NZ");
CultureInfo.DefaultThreadCurrentCulture = nz;
CultureInfo.DefaultThreadCurrentUICulture = nz;
// If you still use explicit thread cultures:
Thread.CurrentThread.CurrentCulture = nz;
Thread.CurrentThread.CurrentUICulture = nz;
#endif

var builder = WebApplication
    .CreateBuilder(args);

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
builder.Services.AddSingleton<IEmailManager, EmailManager>();
builder.Services.AddSingleton<IUserManager, UserManager>();
builder.Services.AddSingleton<IEmailQueueService, EmailQueueService>(); 

builder.Services.AddOptions<CalendarImporterOptions>()
    .Bind(builder.Configuration.GetSection(CalendarImporterOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<AttendanceOptions>()
    .Bind(builder.Configuration.GetSection(AttendanceOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<EmailManagerOptions>()
    .Bind(builder.Configuration.GetSection(EmailManagerOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<CalendarImportService>();
builder.Services.AddHostedService(s => (EmailQueueService)s.GetRequiredService<IEmailQueueService>());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
app.MapControllers(); // Map API controllers

// Verify database exists
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!dbContext.Database.EnsureCreated()) // Creates the database schema if it doesn't exist
    {
        // Apply all patches
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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

app.MapPost("/account/logout", async (HttpContext http) => {
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.Run();
