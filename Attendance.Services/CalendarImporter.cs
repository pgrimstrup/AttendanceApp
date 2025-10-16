using System.Diagnostics;
using Attendance.Data;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Attendance.Services;

public interface ICalendarImporter
{
    Task ImportCalendarAsync();
}

public class CalendarImporter : ICalendarImporter
{
    readonly CalendarImporterOptions _options;
    readonly ILogger<CalendarImporter> _logger;
    readonly AppDbContext _context;

    public CalendarImporter(IOptions<CalendarImporterOptions> options, ILogger<CalendarImporter> logger, AppDbContext context)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task ImportCalendarAsync()
    {
        // Implementation for importing calendar from the provided URL
        if (String.IsNullOrWhiteSpace(_options.SourceUrl))
        {
            _logger.LogWarning("SourceUrl is not configured. Skipping calendar import.");
            return;
        }
        _logger.LogInformation("Importing calendar from {SourceUrl}", _options.SourceUrl);

        // Create an HttpClient to fetch the calendar data
        using var httpClient = new HttpClient();
        var calendarData = await httpClient.GetStringAsync(_options.SourceUrl);

        // Deserialize the iCal data using iCal.NET
        var calendar = Calendar.Load(calendarData);
        if (calendar == null)
            return;


        var yearStart = new DateOnly(DateTime.Today.Year, 7, 1);
        var yearEnd = new DateOnly(DateTime.Today.Year + 2, 7, 1);

        var categories = _context.CalendarCategories.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        var days = _context.CalendarDays
            .Where(d => d.Date >= yearStart && d.Date < yearEnd)
            .ToDictionary(d => d.Date);

        // Populate all dates for the two-year period
        DateOnly date = yearStart;
        while(date < yearEnd)
        {
            if(!days.TryGetValue(date, out var calDay))
            {
                calDay = new CalendarDay {
                    Date = date                    
                };
                days[date] = calDay;
                await _context.CalendarDays.AddAsync(calDay);
            }

            // Reset the events for the day, but we don't cleate the IsClosed flag as this
            // system may override the calendar for specific closed days.
            calDay.Events.Clear();
            calDay.IsPistolDay = false;

            date = date.AddDays(1);
        }


        CalDateTime startDate = new CalDateTime(yearStart);
        CalDateTime endDate = new CalDateTime(yearEnd);
        foreach (var o in calendar.GetOccurrences(startDate).TakeWhileBefore(endDate))
        {
            if (o.Source is CalendarEvent evt)
            {
                // Ensure that the category exists in the database, and update the last used date
                var location = evt.Categories.FirstOrDefault();
                var category = evt.Summary;
                if (!String.IsNullOrWhiteSpace(category) && !String.IsNullOrWhiteSpace(location))
                {
                    if (!categories.TryGetValue(category, out var cat))
                    {
                        cat = new CalendarCategory {
                            Name = category
                        };
                        categories[category] = cat;
                        await _context.CalendarCategories.AddAsync(cat);
                    }
                    cat.LastEvent = o.Period.StartTime.Date;

                    // Find the CalendarDay for this occurrence and add the event
                    date = o.Period.StartTime.Date;
                    do
                    {
                        if (days.TryGetValue(date, out var calDay))
                        {
                            calDay.Events.Add(new CalendarEntry {
                                StartTime = date.ToDateTime(o.Period.StartTime.Time.GetValueOrDefault()),
                                Name = category,
                                Location = location,
                                IsPistolEvent = cat.IsPistolEvent
                            });
                            calDay.IsPistolDay |= cat.IsPistolEvent;
                            calDay.IsClosedDay |= cat.IsClosedEvent;
                        }
                        date = date.AddDays(1);

                    } while (o.Period.EffectiveEndTime != null && date < o.Period.EffectiveEndTime.Date);
                }
            }
        }

        await _context.SaveChangesAsync();
    }
}
