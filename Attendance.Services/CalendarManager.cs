using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface ICalendarManager
{
    Task<CalendarDay[]> GetCalendarDays(int year, int month);
    Task<CalendarCategory[]> GetCalendarCategories(int year, int month);
    Task UpdateCalendarCategory(CalendarCategory category);
    Task<bool> SetRangeClosed(DateOnly date, bool isClosed);
}

public class CalendarManager : ICalendarManager
{
    readonly IServiceProvider _services;
    readonly ILogger<CalendarManager> _logger;

    public CalendarManager(IServiceProvider services, ILogger<CalendarManager> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<CalendarDay[]> GetCalendarDays(int year, int month)
    {
        DateTime startDate = new DateTime(year, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);

        while(startDate.DayOfWeek != DayOfWeek.Monday)
            startDate = startDate.AddDays(-1);
        while(endDate.DayOfWeek != DayOfWeek.Sunday)
            endDate = endDate.AddDays(1);

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.CalendarDays
            .AsNoTracking()
            .Where(cd => cd.Date >= DateOnly.FromDateTime(startDate) && cd.Date <= DateOnly.FromDateTime(endDate))
            .ToArrayAsync();
    }

    public async Task<CalendarCategory[]> GetCalendarCategories(int year, int month)
    {
        DateTime startDate = new DateTime(year, month, 1);
        DateTime endDate = startDate.AddMonths(1).AddDays(-1);
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.CalendarCategories
            .AsNoTracking()
            .ToArrayAsync();
    }

    public async Task<bool> SetRangeClosed(DateOnly date, bool isClosed)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var calendarDay = await context.CalendarDays.FindAsync(date);

        bool wasClosed = false;
        if (calendarDay == null)
        {
            calendarDay = new CalendarDay
            {
                Date = date,
                IsClosedDay = isClosed
            };
            await context.CalendarDays.AddAsync(calendarDay);
        }
        else
        {
            wasClosed = calendarDay.IsClosedDay;
            calendarDay.IsClosedDay = isClosed;
            context.CalendarDays.Update(calendarDay);
        }

        try
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("Calendar Update: {Date:dd/MM/yyyy} - IsClosed = {isClosed}", date, isClosed);
            return calendarDay.IsClosedDay;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating calendar day for {Date:dd/MM/yyyy}", date);
            return wasClosed;
        }
    }

    public async Task UpdateCalendarCategory(CalendarCategory category)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var importer = scope.ServiceProvider.GetRequiredService<ICalendarImporter>();

        var cat = await context.CalendarCategories.FirstOrDefaultAsync(cc => cc.Name == category.Name);
        if (cat == null)
        {
            await context.CalendarCategories.AddAsync(category);
        }
        else
        {
            cat.Color = category.Color;
            cat.IsPistolEvent  = category.IsPistolEvent;
            cat.IsClosedEvent = category.IsClosedEvent;
            context.CalendarCategories.Update(cat);
        }

        try
        {
            await context.SaveChangesAsync();
            await importer.ImportCalendarAsync();

            _logger.LogInformation("Calendar Category Updated: {Name}", category.Name);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating calendar category for {Name}", category.Name);
        }
    }

}
