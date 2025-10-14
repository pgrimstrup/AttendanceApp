using Attendance.Data;

namespace Attendance.ViewModels;

public class HomeViewModel
{
    DateTime _startTime;
    DateTime _endTime;
    DateTime _lastUpdated;

    public int Year { get; set; }

    public HomeViewModel(int? year)
    {
        Year = year ?? DateTime.Now.Year;
        _startTime = new DateTime(Year, 7, 1);
        _endTime = new DateTime(Year + 1, 7, 1).AddDays(-1);
        if(_endTime > DateTime.Today)
            _endTime = DateTime.Today.AddDays(-1);

        _lastUpdated = DateTime.Now;

        IPSC = new();
        ISSF = new();
        CAS = new();
        MultiGun = new();
    }

    public string YearTitle => $"{Year}/{Year+1}";

    public DateTime StartDate => _startTime;

    public DateTime EndDate => _endTime;

    public DateTime LastUpdated => _lastUpdated;

    public List<AttendanceSummaryRecord> IPSC { get; }
    public List<AttendanceSummaryRecord> ISSF { get; }
    public List<AttendanceSummaryRecord> CAS { get; }
    public List<AttendanceSummaryRecord> MultiGun { get; }

}
