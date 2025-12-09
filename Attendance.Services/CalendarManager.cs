using System.Reflection.Metadata.Ecma335;
using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface ICalendarManager
{
    Task<CalendarDay[]> GetCalendarDays(DateTime currentTime);
    Task<CalendarDay[]> GetCalendarDays(int year, int month);
    Task<CalendarCategory[]> GetCalendarCategories(int year, int month);
    Task UpdateCalendarCategory(CalendarCategory category);
    Task SetRangeClosed(CalendarDay day);
    Task SetRangeOpen(CalendarDay day);
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

    public async Task<CalendarDay[]> GetCalendarDays(DateTime currentTime)
    {
        DateTime startDate = new DateTime(currentTime.Year, 7, 1);
        if (DateTime.Now.Month <= 6)
            startDate = startDate.AddYears(-1);

        DateTime endDate = startDate.AddYears(1).AddDays(-1);

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.CalendarDays
            .AsNoTracking()
            .Where(cd => cd.Date >= DateOnly.FromDateTime(startDate) && cd.Date <= DateOnly.FromDateTime(endDate))
            .ToArrayAsync();
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


    public async Task SetRangeClosed(CalendarDay source)
    {
        if(source.IsPistolDay && !source.IsClosedDay)
        {
            source.IsOpenOverride = false;
            source.IsClosedOverride = true;
        }
        else
        {
            source.IsOpenOverride = false;
            source.IsClosedOverride = false;
        }
        await UpdateCalendarDay(source);
    }

    public async Task SetRangeOpen(CalendarDay source)
    {
        if (source.IsPistolDay && !source.IsClosedDay)
        {
            source.IsOpenOverride = false;
            source.IsClosedOverride = false;
        }
        else
        {
            source.IsOpenOverride = true;
            source.IsClosedOverride = false;
        }
        await UpdateCalendarDay(source);
    }

    private async Task UpdateCalendarDay(CalendarDay source)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var calendarDay = await context.CalendarDays.FindAsync(source.Date);

        if (calendarDay == null)
        {
            calendarDay = new CalendarDay {
                Date = source.Date,
                IsPistolDay = source.IsPistolDay,
                IsClosedDay = source.IsClosedDay,
                IsOpenOverride = source.IsOpenOverride,
                IsClosedOverride = source.IsClosedOverride
            };
            await context.CalendarDays.AddAsync(calendarDay);
        }
        else
        {
            calendarDay.IsOpenOverride = source.IsOpenOverride;
            calendarDay.IsClosedOverride = source.IsClosedOverride;
        }

        await context.SaveChangesAsync();
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
