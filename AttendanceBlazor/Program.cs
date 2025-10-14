using Attendance.Data;
using Attendance.Services;
using AttendanceBlazor.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Using API controllers
builder.Services.AddRazorComponents() // Using interactive blazor pages
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped<IEntraPassImporter, EntraPassImporter>();
builder.Services.AddScoped<ISportyRegistrationImporter, SportyRegistrationImporter>();
builder.Services.AddScoped<ICalendarManager, CalendarManager>();
builder.Services.AddScoped<IAttendanceManager, AttendanceManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapControllers(); // Map API controllers

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

app.Run();
