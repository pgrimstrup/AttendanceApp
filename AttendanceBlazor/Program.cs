using System.Globalization;
using Attendance.Data;
using Attendance.Services;
using AttendanceBlazor.Components;
using AttendanceBlazor.HostedServices;
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

// Add services to the container.
builder.Services.AddControllers(); // Using API controllers
builder.Services.AddRazorComponents() // Using interactive blazor pages
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IEntraPassImporter, EntraPassImporter>();
builder.Services.AddScoped<ISportyRegistrationImporter, SportyRegistrationImporter>();
builder.Services.AddScoped<ICalendarImporter, CalendarImporter>();

builder.Services.AddSingleton<ICalendarManager, CalendarManager>();
builder.Services.AddSingleton<IAttendanceManager, AttendanceManager>();

builder.Services.AddOptions<CalendarImporterOptions>()
    .Bind(builder.Configuration.GetSection(CalendarImporterOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddHostedService<CalendarImportService>();

var app = builder.Build();

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

app.Run();
