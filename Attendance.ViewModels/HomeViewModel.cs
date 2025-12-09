using Attendance.Data;

namespace Attendance.ViewModels;

public class HomeViewModel
{

    public int Year { get; set; }
    public DateTime LastUpdated { get; set; }
    public int MatchDaysDone { get; set; }
    public int MatchDaysRemaining { get; set; }
    public int WarningThreshold { get; set; }
    public int RequiredCount { get; set; }


    public HomeViewModel(int? year)
    {
        Year = year ?? (DateTime.Today.Month >= 7 ? DateTime.Today.Year : (DateTime.Today.Year - 1));
        StartDate = new DateTime(Year, 7, 1);
        EndDate = new DateTime(Year + 1, 7, 1);

        LastUpdated = DateTime.Now;
        RequiredCount = 12;
        WarningThreshold = 3;

        IPSC = new();
        ISSF = new();
        CAS = new();
        MultiGun = new();
    }

    public string YearTitle => $"{Year}/{Year+1}";

    public DateTime StartDate { get; }

    public DateTime EndDate { get; }


    public List<AttendanceSummaryRecord> IPSC { get; }
    public List<AttendanceSummaryRecord> ISSF { get; }
    public List<AttendanceSummaryRecord> CAS { get; }
    public List<AttendanceSummaryRecord> MultiGun { get; }

}
