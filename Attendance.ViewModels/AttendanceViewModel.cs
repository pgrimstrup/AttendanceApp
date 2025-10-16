using Attendance.Data;

namespace Attendance.ViewModels;

public class AttendanceViewModel
{
    readonly AttendanceRecord _record;

    public AttendanceViewModel(AttendanceRecord record)
    {
        _record = record;
    }

    public string EventTitle
    {
        get
        {
            return _record.EventDate.ToString("dddd, d MMMM");
        }
    }

    public DateTime EventDate => _record.EventDate;
    public string EntryTime => _record.EntryTime.HasValue ? _record.EntryTime.Value.ToString("hh:mm tt") : "n/a";
    public string RangeTime => _record.RangeTime.HasValue ? _record.RangeTime.Value.ToString("hh:mm tt") : "";
    public string ExitTime => _record.ExitTime.HasValue ? _record.ExitTime.Value.ToString("hh:mm tt") : "n/a";

    /// <summary>
    /// The overall determination if this attendance record counts toward attendance requirements.
    /// If this is false, then the record is excluded for some reason (e.g. closed day, excluded, no matches, 
    /// not on range, not included, etc)
    /// </summary>
    public bool IsCounted => _record.IsCounted;

    public bool IsPistolDay => _record.IsPistolDay;
    public bool IsClosedDay => _record.IsClosedDay;
    public bool IsAwayEvent => _record.IsAwayEvent;
    public bool IsExcluded => _record.IsExcluded;
    public bool IsIncluded => _record.IsIncluded;
    public bool IsOnRange => _record.RangeTime.HasValue;


    public string AwayLocation => _record.AwayLocation ?? "";
    public string AwayEventName => _record.AwayEventName ?? "";

    public string Name => $"{_record.FirstName} {_record.LastName.Substring(0, 1)}";
    public string CardNumber => _record.SwipedCardNumber;
    public int PersonId => _record.PersonId;

    public string TimeOnRange
    {
        get
        {
            if (_record.EntryTime.HasValue && _record.ExitTime.HasValue)
            {
                var span = _record.ExitTime.Value - _record.EntryTime.Value;
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            }
            else if (_record.EntryTime.HasValue && _record.RangeTime.HasValue)
            {
                var span = _record.RangeTime.Value - _record.EntryTime.Value;
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            }
            else if (_record.RangeTime.HasValue && _record.ExitTime.HasValue)
            {
                var span = _record.ExitTime.Value - _record.RangeTime.Value;
                return $"{(int)span.TotalHours}h {span.Minutes:D2}m";
            }
            else if (_record.RangeTime.HasValue)
            {
                return "0h 00m";
            }
            return "n/a";
        }
    }
}
