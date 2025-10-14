using Attendance.Data;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Services;

public interface IAttendanceManager
{
	Task<AttendanceSummaryRecord[]> GetAttendanceSummariesAsync(DateTime startDate, DateTime endDate);
	Task<AttendanceRecord[]> GetAttendanceAsync(DateTime startDate, DateTime endDate, params string[] pnzNumbers);
	Task<EntraPassImport[]> GetSwipeCardEventsAsync(DateTime startDate, DateTime endDate, params string[] cardNumbers);
}


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
Dates(EventDate, IsMatchDay) AS 
(
	SELECT Date, CASE WHEN RecurringEvents + SpecialEvents > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
	FROM CalendarDay
	WHERE Date >= @StartDate AND Date < @EndDate
)
SELECT p.PersonId, p.FirstName, p.LastName, p.CardNumber RegisteredCardNumber, 
	p.FALNumber, p.PNZNumber, p.Sections,
	Dates.EventDate, Dates.IsMatchDay,
	Entries.EntryTime, Ranges.RangeTime, Exits.ExitTime,
	COALESCE(Entries.EntryCardNumber, Ranges.RangeCardNumber, Exits.ExitCardNumber) SwipedCardNumber,
	COALESCE(Entries.EntryPersonId, Ranges.RangePersonId, Exits.ExitPersonId) SwipedPersonId
FROM SportyImport p
CROSS JOIN Dates
LEFT OUTER JOIN Entries on Entries.EntryPersonId = p.PersonId AND CONVERT(DATE, Entries.EntryTime) = Dates.EventDate
LEFT OUTER JOIN Exits on Exits.ExitPersonId = p.PersonId AND CONVERT(DATE, ExitTime) = Dates.EventDate
LEFT OUTER JOIN Ranges on Ranges.RangePersonId = p.PersonId AND CONVERT(DATE, RangeTime) = Dates.EventDate
WHERE (Entries.EntryTime IS NOT NULL OR Exits.ExitTime IS NOT NULL OR Ranges.RangeTime IS NOT NULL OR Dates.IsMatchDay = 1)
";

	readonly AppDbContext _db;

    public AttendanceManager(AppDbContext db)
	{
		_db = db;
	}

	public async Task<AttendanceRecord[]> GetAttendanceAsync(DateTime startDate, DateTime endDate, params string[] pnzNumbers)
	{
		var parameters = new Dictionary<string, object?>
		{
			{ "@StartDate", startDate },
			{ "@EndDate", endDate }
		};

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
        var result = _db.QueryAsync<AttendanceRecord>(sql, parameters);
		await foreach (var item in result)
			results.Add(item);

		return results.ToArray();
    }

	public async Task<AttendanceSummaryRecord[]> GetAttendanceSummariesAsync(DateTime startDate, DateTime endDate)
	{
		var attendance = await GetAttendanceAsync(startDate, endDate);
		var summaries = new Dictionary<string, AttendanceSummaryRecord>(StringComparer.OrdinalIgnoreCase);
		foreach(var record in attendance)
		{
            // Need to have a valid PNZ number and be in a pistol section to count
            if (string.IsNullOrWhiteSpace(record.PNZNumber) || !record.IsPistolSection)
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

            // Need to have scanned in at the range on a match day to count
            if (!record.IsMatchDay || record.RangeTime == null)
                continue;

            summary.Count++;
        }

        return summaries.Values.ToArray();
    }

    public async Task<EntraPassImport[]> GetSwipeCardEventsAsync(DateTime startDate, DateTime endDate, params string[] cardNumbers)
	{
		var q = from ep in _db.EntraPassImports.AsNoTracking()
				where ep.EventTime >= startDate && ep.EventTime < endDate && cardNumbers.Contains(ep.CardNumber)
				orderby ep.EventTime
                select ep;

		return await q.ToArrayAsync();
    }
}
