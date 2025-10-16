using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface IAttendanceManager
{
	Task<AttendanceSummaryRecord[]> GetAttendanceSummariesAsync(DateTime startDate, DateTime endDate);
	Task<AttendanceRecord[]> GetAttendanceAsync(DateTime startDate, DateTime endDate, params string[] pnzNumbers);
	Task<EntraPassImport[]> GetSwipeCardEventsAsync(DateTime startDate, DateTime endDate, params string[] cardNumbers);
	void FlushCache();

	Task IncludedAsync(int personId, DateOnly eventDate, bool isOnRange);
	Task ExcludedAsync(int personId, DateOnly eventDate, bool isOnRange);

	Task AddAwayEvent(string pnzNumber, AwayEvent awayEvent);
    Task AddAwayEvent(int personId, AwayEvent awayEvent);
	Task DeleteAwayEvent(int personId, DateOnly date, string location, string eventName);
}


/// <summary>
/// Most of the data retrieved here only changes once per day, so we can cache it for a short time to avoid
/// making too many database calls.
/// </summary>
public class AttendanceManager : IAttendanceManager
{
	static string Query = @"
WITH Entries(EntryTime, EntryCardNumber, EntryPersonId) AS 
(
	SELECT MIN(EventTime), CardNumber, PersonId 
	FROM EntraPassImport
	WHERE Location = 'Vehicle Gate Entry'
	  AND EventTime >= @StartDate AND EventTime < @EndDate
	GROUP BY CONVERT(DATE, EventTime), CardNumber, PersonId
),
Exits(ExitTime, ExitCardNumber, ExitPersonId) AS 
(
	SELECT MAX(EventTime), CardNumber, PersonId 
	FROM EntraPassImport
	WHERE Location = 'Vehicle Gate Exit'
	  AND EventTime >= @StartDate AND EventTime < @EndDate
	GROUP BY CONVERT(DATE, EventTime), CardNumber, PersonId
),
Ranges(RangeTime, RangeCardNumber, RangePersonId) AS
(
	SELECT MIN(EventTime), CardNumber, PersonId 
	FROM EntraPassImport
	WHERE Location IN ('Range 1', 'Range 5')
	  AND EventTime >= @StartDate AND EventTime < @EndDate
	GROUP BY CONVERT(DATE, EventTime), CardNumber, PersonId
),
Dates(EventDate, IsPistolDay, IsClosedDay) AS 
(
	SELECT Date, IsPistolDay, IsClosedDay
	FROM CalendarDay
	WHERE Date >= @StartDate AND Date < @EndDate
),
Overrides(PersonId, EventDate, IsExcluded, IsIncluded) AS
(
	SELECT PersonId, EventDate, IsExcluded, IsIncluded
	FROM OverrideEvent 
	WHERE EventDate >= @StartDate AND EventDate < @EndDate
),
Away(PersonId, EventDate, IsAwayEvent, AwayLocation, AwayEventName) AS 
(
	SELECT PersonId, EventDate, CAST(1 AS BIT), AwayEvent.Location, AwayEvent.EventName
	FROM Dates
	CROSS JOIN AwayEvent
	WHERE Dates.EventDate >= AwayEvent.StartDate AND Dates.EventDate <= AwayEvent.EndDate
)
SELECT p.PersonId, p.FirstName, p.LastName, p.CardNumber RegisteredCardNumber, 
	p.FALNumber, p.PNZNumber, p.Sections, Dates.EventDate, Dates.IsPistolDay, Dates.IsClosedDay,
	COALESCE(Overrides.IsExcluded, CAST(0 AS BIT)) IsExcluded, 
	COALESCE(Overrides.IsIncluded, CAST(0 AS BIT)) IsIncluded,
	COALESCE(Away.IsAwayEvent, CAST(0 AS BIT)) IsAwayEvent, Away.AwayLocation, Away.AwayEventName,
	Entries.EntryTime, Ranges.RangeTime, Exits.ExitTime,
	COALESCE(Entries.EntryCardNumber, Ranges.RangeCardNumber, Exits.ExitCardNumber) SwipedCardNumber,
	COALESCE(Entries.EntryPersonId, Ranges.RangePersonId, Exits.ExitPersonId) SwipedPersonId
FROM SportyImport p
CROSS JOIN Dates
left outer join Overrides on Overrides.PersonId = p.PersonId AND Overrides.EventDate = Dates.EventDate
LEFT OUTER JOIN Entries on Entries.EntryPersonId = p.PersonId AND CONVERT(DATE, Entries.EntryTime) = Dates.EventDate
LEFT OUTER JOIN Exits on Exits.ExitPersonId = p.PersonId AND CONVERT(DATE, ExitTime) = Dates.EventDate
LEFT OUTER JOIN Ranges on Ranges.RangePersonId = p.PersonId AND CONVERT(DATE, RangeTime) = Dates.EventDate
LEFT OUTER JOIN Away on Away.PersonId = p.PersonId AND Dates.EventDate = Away.EventDate
WHERE (Entries.EntryTime IS NOT NULL OR Exits.ExitTime IS NOT NULL OR Ranges.RangeTime IS NOT NULL OR Dates.IsPistolDay = 1 OR IsAwayEvent = 1)
";

	readonly IServiceProvider _services;
	readonly IMemoryCache _cache;
	readonly List<string> _keys = new();
	readonly ILogger _logger;
	TimeSpan _cacheTimeToLive = TimeSpan.FromHours(1);

    public AttendanceManager(IServiceProvider services, IMemoryCache cache, ILogger<AttendanceManager> logger)
	{
		_services = services;
        _cache = cache;
		_logger = logger;
    }

	public void FlushCache()
	{
		_logger.LogInformation($"Flushing Attendance Manager Cache: {_keys.Count} keys removed");
		foreach(var key in _keys)
			_cache.Remove(key);
		_keys.Clear();
    }


    public async Task<AttendanceRecord[]> GetAttendanceAsync(DateTime startDate, DateTime endDate, params string[] pnzNumbers)
	{
		var parameters = new Dictionary<string, object?>
		{
			{ "@StartDate", startDate },
			{ "@EndDate", endDate }
		};

		string key = $"Attendance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{String.Join("+", pnzNumbers)}";
		if(_cache.TryGetValue<AttendanceRecord[]>(key, out var cached) && cached != null)
			return cached;

        // Since we need to use string concatenation to build the IN clause, we need to ensure
        // that all values are SQL parameters.
        var sql = Query;
		if(pnzNumbers != null && pnzNumbers.Length > 0)
		{
			int index = 1;
			var parameterNames = new List<string>();
			foreach(var pnzNumber in pnzNumbers.Distinct())
			{
				parameters.Add($"@P{index}", pnzNumber.Trim());
				parameterNames.Add($"@P{index}");
				index++;
            }

			sql += " AND p.PNZNumber IN (" + string.Join(", ", parameterNames) + ")";
		}

		var results = new List<AttendanceRecord>();

		using var scope = _services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var result = context.QueryAsync<AttendanceRecord>(sql, parameters);
		await foreach (var item in result)
			results.Add(item);

		_cache.Set(key, results.ToArray(), _cacheTimeToLive);
        if (!_keys.Contains(key))
            _keys.Add(key);
        return results.ToArray();
    }

	public async Task<AttendanceSummaryRecord[]> GetAttendanceSummariesAsync(DateTime startDate, DateTime endDate)
	{
        string key = $"AttendanceSummary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}";
        if (_cache.TryGetValue<AttendanceSummaryRecord[]>(key, out var cached) && cached != null)
            return cached;
        
		var attendance = await GetAttendanceAsync(startDate, endDate);
		var summaries = new Dictionary<string, AttendanceSummaryRecord>(StringComparer.OrdinalIgnoreCase);
		foreach(var record in attendance)
		{
            // Need to have a valid PNZ number to count
            if (string.IsNullOrWhiteSpace(record.PNZNumber))
				continue;

			if(!summaries.TryGetValue(record.PNZNumber, out var summary))
			{
				summary = new AttendanceSummaryRecord
				{
					PNZNumber = record.PNZNumber,
					Sections = record.Sections,
                    Count = 0
				};
				summaries.Add(record.PNZNumber, summary);
			}

            // Need to have scanned in at the range on a match day to count.
            // Also count away matches, which are marked as match days but don't have range scans.
            // Can be manually overriden by IsIncluded/IsExcluded.
            if (record.IsPistolDay && !record.IsClosedDay && !record.IsExcluded)
			{
				if(record.RangeTime.HasValue || record.IsIncluded || record.IsAwayEvent)
					summary.Count++;
            }
			
        }

		_cache.Set(key, summaries.Values.ToArray(), _cacheTimeToLive);
        if (!_keys.Contains(key))
            _keys.Add(key);
        return summaries.Values.ToArray();
    }

    public async Task<EntraPassImport[]> GetSwipeCardEventsAsync(DateTime startDate, DateTime endDate, params string[] cardNumbers)
	{
		string key = $"SwipeCardEvents_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{String.Join("+", cardNumbers)}";
		if (_cache.TryGetValue<EntraPassImport[]>(key, out var cached) && cached != null)
			return cached;

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var q = from ep in context.EntraPassImports.AsNoTracking()
				where ep.EventTime >= startDate && ep.EventTime < endDate && cardNumbers.Contains(ep.CardNumber)
				orderby ep.EventTime
                select ep;

		var result = await q.ToArrayAsync();
		_cache.Set(key, result, _cacheTimeToLive);
		if(!_keys.Contains(key))
			_keys.Add(key);
        return result;
    }

    public async Task IncludedAsync(int personId, DateOnly eventDate, bool isOnRange)
	{
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await context.OverrideEvents.FindAsync(personId, eventDate);
        if (existing == null)
		{
			existing = new OverrideEvent
			{
				PersonId = personId,
				EventDate = eventDate
			};
			await context.OverrideEvents.AddAsync(existing);
        }
        
		// IsIncluded should only be set if the user was not currently on the range (ie, no RangeDate)
		existing.IsIncluded = !isOnRange;
        existing.IsExcluded = false;
        await context.SaveChangesAsync();

        FlushCache();
	}
    public async Task ExcludedAsync(int personId, DateOnly eventDate, bool isOnRange)
	{
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existing = await context.OverrideEvents.FindAsync(personId, eventDate);
        if (existing == null)
        {
            existing = new OverrideEvent {
                PersonId = personId,
                EventDate = eventDate
            };
            await context.OverrideEvents.AddAsync(existing);
        }

		// IsExluded should only be set if the user was on the range (ie, has a RangeDate)
        existing.IsIncluded = false;
        existing.IsExcluded = isOnRange;
       await context.SaveChangesAsync();

        FlushCache();
    }

	public async Task AddAwayEvent(string pnzNumber, AwayEvent awayEvent)
	{
		using var scope = _services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		// Find the person by PNZ number
		var person = await context.SportyImports.FirstOrDefaultAsync(i => i.PNZNumber == pnzNumber);
		if (person == null)
			throw new ArgumentException($"No person found with PNZ number {pnzNumber}");

		awayEvent.PersonId = person.PersonId;
        await context.AwayEvents.AddAsync(awayEvent);
		await context.SaveChangesAsync();
		FlushCache();
    }

	public async Task AddAwayEvent(int personId, AwayEvent awayEvent)
	{
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        awayEvent.PersonId = personId;
        await context.AwayEvents.AddAsync(awayEvent);
        await context.SaveChangesAsync();
        FlushCache();
    }

    public async Task DeleteAwayEvent(int personId, DateOnly date, string location, string eventName)
	{
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var away = await context.AwayEvents.FirstOrDefaultAsync(a => a.PersonId == personId && a.StartDate <= date && a.EndDate >= date && a.Location == location && a.EventName == eventName);
		if(away != null)
		{
			context.AwayEvents.Remove(away);
			await context.SaveChangesAsync();
			FlushCache();
        }
    }
}
