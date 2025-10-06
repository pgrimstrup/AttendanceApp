using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface ICalendarManager
{
    Task<RecurringEvent[]> GetRecurringEvents();
    Task<SpecialEvent[]> GetSpecialEvents();
    Task<CalendarDay[]> GetCalendarDays(int year, int month);

    Task<bool> AddOrUpdateRecurringEvent(RecurringEvent e);
    Task<bool> EndRecurringEvent(int id, DateTime endDate);
    Task<bool> DeleteRecurringEvent(int id);
    Task<bool> ResumeRecurringEvent(int id);

    Task<bool> AddOrUpdateSpecialEvent(SpecialEvent e);
    Task<bool> DeleteSpecialEvent(int id);
}

public class CalendarManager : ICalendarManager
{
    readonly AppDbContext _context;
    readonly ILogger _logger;

    public CalendarManager(AppDbContext context, ILogger<CalendarManager> logger)
    {
        _context = context;
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

        return await _context.CalendarDays
            .AsNoTracking()
            .Where(cd => cd.Date >= DateOnly.FromDateTime(startDate) && cd.Date <= DateOnly.FromDateTime(endDate))
            .ToArrayAsync();
    }

    public async Task<RecurringEvent[]> GetRecurringEvents()
    {
        DateOnly startDate = new DateOnly(DateTime.Today.Year, 7, 1);
        if (DateTime.Today.Month >= 7)
            startDate = startDate.AddYears(-1);

        startDate = startDate.AddDays(-7);
        return await _context.RecurringEvents
            .AsNoTracking()
            .Where(e => e.EndDate == null || e.EndDate >= startDate)
            .ToArrayAsync();
    }

    public async Task<SpecialEvent[]> GetSpecialEvents()
    {
        DateOnly startDate = new DateOnly(DateTime.Today.Year, 7, 1);
        if (DateTime.Today.Month >= 7)
            startDate = startDate.AddYears(-1);

        startDate = startDate.AddDays(-7);
        return await _context.SpecialEvents
            .AsNoTracking()
            .Where(e => e.EndDate >= startDate)
            .ToArrayAsync();
    }


    public async Task<bool> AddOrUpdateRecurringEvent(RecurringEvent e)
    {
        try
        {
            var found = await _context.RecurringEvents.FindAsync(e.Id);
            if (found == null)
            {
                await _context.RecurringEvents.AddAsync(e);
            }
            else
            {
                found.StartDate = e.StartDate;
                found.Frequency = e.Frequency;
                found.DayOfWeek = e.DayOfWeek;
                found.Description = e.Description;
            }
            await _context.SaveChangesAsync();

            await CalculateCalendarDays();
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to Add or Update Recurring Event");
            return false;
        }
    }

    public async Task<bool> AddOrUpdateSpecialEvent(SpecialEvent e)
    {
        try
        {
            var found = await _context.SpecialEvents.FindAsync(e.Id);
            if (found == null)
            {
                await _context.SpecialEvents.AddAsync(e);
            }
            else
            {
                found.StartDate = e.StartDate;
                found.EndDate = e.EndDate;
                found.Description = e.Description;
            }
            await _context.SaveChangesAsync();

            await CalculateCalendarDays();
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to Add or Update Special Event");
            return false;
        }
    }

    public async Task<bool> DeleteRecurringEvent(int id)
    {
        try
        {
            var found = _context.RecurringEvents.Find(id);
            if (found != null)
            {
                _context.RecurringEvents.Remove(found);

                await _context.SaveChangesAsync();

                await CalculateCalendarDays();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Delete Recurring Event");
            return false;
        }
    }

    public async Task<bool> ResumeRecurringEvent(int id)
    {
        try
        {
            var found = _context.RecurringEvents.Find(id);
            if (found != null)
            {
                found.EndDate = null;

                await _context.SaveChangesAsync();

                await CalculateCalendarDays();
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Resume Recurring Event");
            return false;
        }
    }


    public async Task<bool> EndRecurringEvent(int id, DateTime endDate)
    {
        try
        {
            var found = _context.RecurringEvents.Find(id);
            if (found != null)
            {
                DateOnly end = DateOnly.FromDateTime(endDate);
                found.EndDate = end;

                await _context.SaveChangesAsync();

                await CalculateCalendarDays();
            }
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to End Recurring Event");
            return false;
        }
    }

    public async Task<bool> DeleteSpecialEvent(int id)
    {
        try
        {
            var found = _context.SpecialEvents.Find(id);
            if (found != null)
            {
                _context.SpecialEvents.Remove(found);
                await _context.SaveChangesAsync();

                await CalculateCalendarDays();
            }
            return true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Failed to Delete Special Event");
            return false;
        }
    }

    private async Task CalculateCalendarDays()
    {
        DateTime startTime = new DateTime(DateTime.Today.Year, 7, 1);
        if (DateTime.Today.Month <= 6)
            startTime = startTime.AddYears(-1);
        DateTime endTime = new DateTime(DateTime.Today.Year + 1, 7, 1).AddDays(-1);
        if(DateTime.Today.Month >= 7)
            endTime = endTime.AddYears(1);

        DateOnly fromDate = DateOnly.FromDateTime(startTime).AddDays(-7);
        DateOnly toDate = DateOnly.FromDateTime(endTime).AddDays(7);
        var days = await _context.CalendarDays
            .Where(cd => cd.Date >= fromDate)
            .ToDictionaryAsync(d =>d.Date);

        var specialEvents = await GetSpecialEvents();
        var recurringEvents = await GetRecurringEvents();

        DateOnly date = fromDate;
        List<string> descriptions = new();
        while(date <= toDate)
        {
            if(!days.TryGetValue(date, out var cday))
            {
                cday = new CalendarDay {
                    Date = date
                };
                await _context.CalendarDays.AddAsync(cday);
                days[date] = cday;
            }

            descriptions.Clear();
            cday.SpecialEvents = CountSpecialEvents(date, specialEvents, descriptions);
            cday.RecurringEvents = CountRecurringEvents(date, recurringEvents, descriptions);
            if (descriptions.Count == 0)
                cday.Description = null;
            else
                cday.Description = string.Join("; ", descriptions);

            date = date.AddDays(1);
        }

        await _context.SaveChangesAsync();

    }

    private int CountRecurringEvents(DateOnly date, RecurringEvent[] recurringEvents, List<string> descriptions)
    {
        int count = 0;
        foreach(var e in recurringEvents)
        {
            if (e.StartDate <= date && (e.EndDate == null || e.EndDate >= date) && e.DayOfWeek == date.DayOfWeek)
            {
                bool matches = e.Frequency switch {
                    EventFrequency.EveryWeek => true,
                    EventFrequency.FirstWeek => date.Day <= 7,
                    EventFrequency.SecondWeek => date.Day >= 8 && date.Day <= 14,
                    EventFrequency.ThirdWeek => date.Day >= 15 && date.Day <= 21,
                    EventFrequency.FourthWeek => date.Day >= 22 && date.Day <= 28,
                    _ => false
                };
                if (matches)
                {
                    if (!string.IsNullOrWhiteSpace(e.Description))
                        descriptions.Add(e.Description!);
                    count++;
                }
            }
        }
        return count;
    }

    private int CountSpecialEvents(DateOnly date, SpecialEvent[] specialEvents, List<string> descriptions)
    {
        int count = 0;
        foreach(var e in specialEvents)
        {
            if (e.StartDate <= date && e.EndDate >= date)
            {
                if (!string.IsNullOrWhiteSpace(e.Description))
                    descriptions.Add(e.Description!);
                count++;
            }
        }

        return count;
    }
}
