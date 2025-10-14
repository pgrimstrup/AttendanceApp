using Attendance.Data;

namespace Attendance.Services;

public interface IAttendanceManager
{
	IAsyncEnumerable<AttendanceRecord> GetAttendanceAsync(DateTime startDate, DateTime endDate, params int[] personId);
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
WHERE (Entries.EntryTime IS NOT NULL OR Exits.ExitTime IS NOT NULL OR Ranges.RangeTime IS NOT NULL)
";

	readonly AppDbContext _db;

    public AttendanceManager(AppDbContext db)
	{
		_db = db;
	}

	public async IAsyncEnumerable<AttendanceRecord> GetAttendanceAsync(DateTime startDate, DateTime endDate, params int[] personId)
	{
		var parameters = new Dictionary<string, object?>
		{
			{ "@StartDate", startDate },
			{ "@EndDate", endDate }
		};

        // Since we need to use string concatenation to build the IN clause, we need to ensure
		// that all values are SQL parameters.
        var sql = Query;
		if(personId != null && personId.Length > 0)
		{
			int index = 1;
			var idList = new List<string>();
			foreach(var id in personId.Distinct())
			{
				parameters.Add($"@Id{index}", id);
				idList.Add($"@Id{index}");
				index++;
            }

			sql += " AND p.PersonId IN (" + string.Join(", ", idList) + ")";
		}

		var result = _db.QueryAsync<AttendanceRecord>(sql, parameters);
		await foreach (var item in result)
			yield return item;
    }
}
