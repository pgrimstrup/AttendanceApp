using System.Text;
using Attendance.Data;
using CSVFile;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface IEntraPassImporter
{
    Task<bool> Import(Stream stream);
    Task<DateTime> GetLastImportDateTimeAsync();
    Task<byte[]> CreateTestData(Stream stream);

}

public class EntraPassImporter : IEntraPassImporter
{
    readonly ILogger _logger;
    readonly AppDbContext _dbContext;
    readonly IAttendanceManager _attendanceManager;

    public EntraPassImporter(AppDbContext dbContext, ILogger<EntraPassImporter> logger, IAttendanceManager attendanceManager)
    {
        _logger = logger;
        _dbContext = dbContext;
        _attendanceManager = attendanceManager;
    }

    public async Task<DateTime> GetLastImportDateTimeAsync()
    {
        var dt = await _dbContext.ImportedFiles
            .Where(f => f.FileType == "EntraPass")
            .OrderByDescending(f => f.DateImported)
            .Select(f => f.DateImported)
            .FirstOrDefaultAsync();

        return dt.ToLocalTime().DateTime;
    }

    public async Task<bool> Import(Stream stream)
    {
        using var csv = new CSVReader(stream);
        int eventTimeIndex = csv.Headers.IndexOf("Date and Time", StringComparer.OrdinalIgnoreCase);
        int cardNumberIndex = csv.Headers.IndexOf("Card number", StringComparer.OrdinalIgnoreCase);
        int cardUserNameIndex = csv.Headers.IndexOf("Card user name", StringComparer.OrdinalIgnoreCase);
        int cardInfo1Index = csv.Headers.IndexOf("CardInfo1", StringComparer.OrdinalIgnoreCase);
        int cardInfo2Index = csv.Headers.IndexOf("CardInfo2", StringComparer.OrdinalIgnoreCase);
        int eventMessageIndex = csv.Headers.IndexOf("Event message", StringComparer.OrdinalIgnoreCase);
        int endDateIndex = csv.Headers.IndexOf("End date", StringComparer.OrdinalIgnoreCase);
        int eventInfo1Index = csv.Headers.IndexOf("Event info #1", StringComparer.OrdinalIgnoreCase);

        if (eventTimeIndex < 0 || cardNumberIndex < 0 || cardUserNameIndex < 0 || cardInfo1Index < 0 ||
            cardInfo2Index < 0 || eventMessageIndex < 0 || endDateIndex < 0 || eventInfo1Index < 0)
            return false;

        var info = new ImportedFile {
            FileType = "EntraPass",
            DateImported = DateTimeOffset.UtcNow,
            FileContent = "CSV"
        };
        await _dbContext.ImportedFiles.AddAsync(info);

        foreach (var line in csv)
        {
            try
            {
                var data = new EntraPassImport {
                    EventTime = Convert.ToDateTime(line[eventTimeIndex]),
                    CardNumber = line[cardNumberIndex]?.Trim() ?? "",
                    CardUserName = line[cardUserNameIndex]?.Trim(),
                    CardInfo1 = line[cardInfo1Index]?.Trim(),
                    CardInfo2 = line[cardInfo2Index]?.Trim(),
                    EventMessage = line[eventMessageIndex],
                    ExpiryDate = Convert.ToDateTime(line[endDateIndex]),
                    Location = line[eventInfo1Index]?.Replace("RRGC-", "")?.Trim(),
                };

                ExtractPersonDetails(data);

                var found = await _dbContext.EntraPassImports.FindAsync(data.EventTime, data.CardNumber);
                if (found == null)
                {
                    // New record
                    await _dbContext.EntraPassImports.AddAsync(data);
                }
                else
                {
                    // Just in case we are re-importing after fixing an issue, we ensure the data is correct
                    found.CardUserName = data.CardUserName;
                    found.EventMessage = data.EventMessage;
                    found.Location = data.Location;
                    found.CardInfo1 = data.CardInfo1;
                    found.CardInfo2 = data.CardInfo2;
                    found.PersonId = data.PersonId;
                    found.Sections = data.Sections;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        await _dbContext.SaveChangesAsync();
        _attendanceManager.FlushCache();
        return true;
    }

    public async Task<byte[]> CreateTestData(Stream stream)
    {
        using var csv = new CSVReader(stream);
        int eventTimeIndex = csv.Headers.IndexOf("Date and Time", StringComparer.OrdinalIgnoreCase);
        int cardNumberIndex = csv.Headers.IndexOf("Card number", StringComparer.OrdinalIgnoreCase);
        int cardUserNameIndex = csv.Headers.IndexOf("Card user name", StringComparer.OrdinalIgnoreCase);
        int cardInfo1Index = csv.Headers.IndexOf("CardInfo1", StringComparer.OrdinalIgnoreCase);
        int cardInfo2Index = csv.Headers.IndexOf("CardInfo2", StringComparer.OrdinalIgnoreCase);
        int eventMessageIndex = csv.Headers.IndexOf("Event message", StringComparer.OrdinalIgnoreCase);
        int endDateIndex = csv.Headers.IndexOf("End date", StringComparer.OrdinalIgnoreCase);
        int eventInfo1Index = csv.Headers.IndexOf("Event info #1", StringComparer.OrdinalIgnoreCase);

        if (eventTimeIndex < 0 || cardNumberIndex < 0 || cardUserNameIndex < 0 || cardInfo1Index < 0 ||
            cardInfo2Index < 0 || eventMessageIndex < 0 || endDateIndex < 0 || eventInfo1Index < 0)
            return Array.Empty<byte>();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Date and Time,Card number,Card user name,CardInfo1,CardInfo2,Event message,End date,Event info #1");

        foreach (var line in csv)
        {
            try
            {
                var data = new EntraPassImport {
                    EventTime = Convert.ToDateTime(line[eventTimeIndex]),
                    CardNumber = line[cardNumberIndex]?.Trim() ?? "",
                    CardUserName = line[cardUserNameIndex]?.Trim(),
                    CardInfo1 = line[cardInfo1Index]?.Trim(),
                    CardInfo2 = line[cardInfo2Index]?.Trim(),
                    EventMessage = line[eventMessageIndex],
                    ExpiryDate = Convert.ToDateTime(line[endDateIndex]),
                    Location = line[eventInfo1Index]?.Replace("RRGC-", "")?.Trim()
                };

                // Only extract my data and replace sensitive data with garbage
                if (data.CardInfo1 != null && data.CardInfo1.Contains("Grimstrup", StringComparison.OrdinalIgnoreCase))
                {
                    data.CardInfo2 = "12345"; // Fake PNZ number
                    sb.AppendLine($"{data.EventTime:o},{data.CardNumber},{data.CardUserName},{data.CardInfo1},{data.CardInfo2},{data.EventMessage},{data.ExpiryDate:o},{data.Location}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private void ExtractPersonDetails(EntraPassImport data)
    {
        if (String.IsNullOrWhiteSpace(data.CardUserName))
            return;

        var parts = data.CardUserName.Trim().Split(' ', 2);
        foreach (string part in parts)
        {
            if (Int32.TryParse(part, out int personId) && personId > 4000000)
            {
                data.PersonId = personId;
                continue;
            }

            switch (part.Trim().ToLower())
            {
                case "ipsc":
                    data.Sections |= SectionTags.IPSC;
                    break;
                case "issf":
                    data.Sections |= SectionTags.ISSF;
                    break;
                case "cas":
                    data.Sections |= SectionTags.CAS;
                    break;
                case "air rifle":
                    data.Sections |= SectionTags.AirRifle;
                    break;
                case "3gun":
                case "multigun":
                    data.Sections |= SectionTags.MultiGun;
                    break;
                case "speed steel":
                    data.Sections |= SectionTags.SpeedSteel;
                    break;
                case "service rifle":
                    data.Sections |= SectionTags.ServiceRifle;
                    break;
                case "black powder":
                    data.Sections |= SectionTags.BlackPowder;
                    break;
                case "sporting rifle":
                    data.Sections |= SectionTags.SportingRifle;
                    break;
                case "sighting in":
                    data.Sections |= SectionTags.SightingIn;
                    break;
                case "smallbore":
                case "smallbore rifle":
                    data.Sections |= SectionTags.SmallboreRifle;
                    break;
            }
        }
    }
}
