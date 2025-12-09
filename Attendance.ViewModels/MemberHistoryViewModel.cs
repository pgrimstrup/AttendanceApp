using Attendance.Data;

namespace Attendance.ViewModels;

public class MemberHistoryViewModel
{
    DateTime _startTime;
    DateTime _endTime;

    public int Year { get; set; }
    public string PNZNumber { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;

    public bool IsPNZMember => !String.IsNullOrWhiteSpace(PNZNumber);

    public MemberHistoryViewModel(int? year)
    {
        Year = year ?? (DateTime.Today.Month >= 7 ? DateTime.Today.Year : (DateTime.Today.Year - 1));
        _startTime = new DateTime(Year, 7, 1);
        _endTime = new DateTime(Year + 1, 7, 1);
        if(_endTime >= DateTime.Today)
            _endTime = DateTime.Today.AddDays(-1);

        Attendance = new();
        SwipeEvents = new();
    }

    public string YearTitle => $"{Year}/{Year + 1}";

    public DateTime StartDate => _startTime;

    public DateTime EndDate => _endTime;

    public DateTime LastUpdated { get; set; }

    public int TotalMatchDays => Attendance.Count(a => a.IsMatchDay);
    public int TotalEventsCounted => Attendance.Count(a => a.IsCounted);

    public List<AttendanceViewModel> Attendance { get; }
    public List<EntraPassImport> SwipeEvents { get; }
}
