using Attendance.Data;
using CSVFile;
using Microsoft.Extensions.Logging;

namespace Attendance.Services;

public interface ISportyRegistrationImporter
{
    Task<bool> Import(Stream stream);
}

public class SportyRegistrationImporter : ISportyRegistrationImporter
{
    readonly ILogger _logger;
    readonly AppDbContext _dbContext;

    public SportyRegistrationImporter(ILogger<EntraPassImporter> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<bool> Import(Stream stream)
    {
        var settings = new CSVSettings {
            HeaderRowIncluded = true,
            LineSeparator = "\n"
        };
        using var csv = new CSVReader(stream, settings);

        int registrationDateIndex = csv.Headers.IndexOf("Date", StringComparer.OrdinalIgnoreCase);
        int personIdIndex = csv.Headers.IndexOf("Person ID", StringComparer.OrdinalIgnoreCase);
        int firstNameIndex = csv.Headers.IndexOf("First name", StringComparer.OrdinalIgnoreCase);
        int lastNameIndex = csv.Headers.IndexOf("Last name", StringComparer.OrdinalIgnoreCase);
        int mobilNumberIndex = csv.Headers.IndexOf("Mobile Ph Number", StringComparer.OrdinalIgnoreCase);
        int emailIndex = csv.Headers.IndexOf("Email", StringComparer.OrdinalIgnoreCase);

        // Mandatory fields
        if (registrationDateIndex < 0 || personIdIndex < 0 || firstNameIndex < 0 || lastNameIndex < 0 || mobilNumberIndex < 0 || emailIndex < 0)
            return false;

        // Optional fields
        int cardNumberIndex = FindCardNumber(csv);
        int falNumberIndex = FindFALNumber(csv);
        int pnzNumberIndex = FindPNZNumber(csv);
        var sectionTag = FindSectionTag(csv);

        foreach (var line in csv)
        {
            try
            {
                var cardNumber = cardNumberIndex < 0 ? "" : line[cardNumberIndex]?.Trim() ?? "";
                var falNumber = falNumberIndex < 0 ? "" : line[falNumberIndex]?.Trim() ?? "";
                var pnzNumber = pnzNumberIndex < 0 ? "" : line[pnzNumberIndex]?.Trim() ?? "";

                var data = new SportyImport {
                    RegistrationDate = Convert.ToDateTime(line[registrationDateIndex]),
                    PersonId = Convert.ToInt32(line[personIdIndex]),
                    FirstName = line[firstNameIndex]?.Trim() ?? "",
                    LastName = line[lastNameIndex]?.Trim() ?? "",
                    CardNumber = CleanCardNumber(cardNumber),
                    FALNumber = falNumber,
                    PNZNumber = CleanPNZNumber(pnzNumber),
                    MobileNumber = CleanMobileNumber(line[mobilNumberIndex]),
                    EmailAddress = line[emailIndex]?.Trim() ?? "",  
                    Sections = sectionTag
                };

                var found = await _dbContext.SportyImports.FindAsync(data.PersonId);
                if (found == null)
                {
                    // New record
                    await _dbContext.SportyImports.AddAsync(data);
                }
                else
                {
                    // Just in case we are re-importing after fixing an issue, we ensure the data is correct
                    found.RegistrationDate = data.RegistrationDate;
                    found.FirstName = data.FirstName;
                    found.LastName = data.LastName;
                    found.MobileNumber = data.MobileNumber;
                    found.EmailAddress = data.EmailAddress;

                    if (cardNumberIndex >= 0)
                        found.CardNumber = data.CardNumber;
                    if(sectionTag != SectionTags.None)
                        found.Sections = data.Sections;
                    if(falNumberIndex >= 0)
                        found.FALNumber = data.FALNumber;
                    if(pnzNumberIndex >= 0)
                        found.PNZNumber = data.PNZNumber;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
            }
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private string CleanMobileNumber(string mobileNumber)
    {
        if (String.IsNullOrWhiteSpace(mobileNumber))
            return mobileNumber.Trim();
        if (mobileNumber.StartsWith("=\""))
            mobileNumber = mobileNumber.TrimStart('=');
        if (mobileNumber.StartsWith("\""))
            mobileNumber = mobileNumber.Trim('"');
        mobileNumber = mobileNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (mobileNumber.StartsWith("+64"))
            mobileNumber = "0" + mobileNumber.Substring(3);
        else if (mobileNumber.StartsWith("64"))
            mobileNumber = "0" + mobileNumber.Substring(2);
        else if (mobileNumber.StartsWith("0064"))
            mobileNumber = "0" + mobileNumber.Substring(4);
        return mobileNumber;
    }

    private string CleanCardNumber(string cardNumber)
    {
        if (String.IsNullOrWhiteSpace(cardNumber))
            return cardNumber.Trim();

        if (cardNumber.StartsWith("=\""))
            cardNumber = cardNumber.TrimStart('=');

        if (cardNumber.StartsWith("\""))
            cardNumber = cardNumber.Trim('"');

        if (cardNumber.StartsWith("XSF", StringComparison.OrdinalIgnoreCase))
            cardNumber = cardNumber.Replace("XSF", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (cardNumber.StartsWith("MSF", StringComparison.OrdinalIgnoreCase))
            cardNumber = cardNumber.Replace("MSF", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (cardNumber.StartsWith("XCF", StringComparison.OrdinalIgnoreCase))
            cardNumber = cardNumber.Replace("XCF", "", StringComparison.OrdinalIgnoreCase).Trim();

        if (cardNumber.StartsWith("O2", StringComparison.OrdinalIgnoreCase) )
            cardNumber = cardNumber.Replace("O2", "02", StringComparison.OrdinalIgnoreCase);

        if (cardNumber.StartsWith("02", StringComparison.OrdinalIgnoreCase) && cardNumber.Contains("O"))
            cardNumber = cardNumber.Replace("O", "0", StringComparison.OrdinalIgnoreCase);

        if (cardNumber.StartsWith("02") && cardNumber.Contains(" "))
            cardNumber = cardNumber.Replace(" ", "");

        if (cardNumber.StartsWith("02") && !cardNumber.Contains(":") && cardNumber.Length == 9)
            cardNumber = cardNumber.Substring(0, 4) + ":" + cardNumber.Substring(4);

        return cardNumber;
    }

    private string CleanPNZNumber(string pnzNumber)
    {
        if (String.IsNullOrWhiteSpace(pnzNumber))
            return pnzNumber;

        if (pnzNumber.StartsWith("=\""))
            pnzNumber = pnzNumber.TrimStart('=');

        if (pnzNumber.StartsWith("\""))
            pnzNumber = pnzNumber.Trim('"');

        if (pnzNumber.StartsWith("PNZ", StringComparison.OrdinalIgnoreCase))
            pnzNumber = pnzNumber.Replace("PNZ", "", StringComparison.OrdinalIgnoreCase).Trim();

        return pnzNumber;
    }

    private int FindCardNumber(CSVReader csv)
    {
        for (int i = 0; i < csv.Headers.Length; i++)
        {
            if (csv.Headers[i].Contains("Gate entry card number", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private SectionTags FindSectionTag(CSVReader csv)
    {
        for (int i = 0; i < csv.Headers.Length; i++)
        {
            if (csv.Headers[i].Contains("This form is for pistol shooters whose PRIMARY section is IPSC Pistol", StringComparison.OrdinalIgnoreCase))
                return SectionTags.IPSC;
            if (csv.Headers[i].Contains("This form is for pistol shooters whose PRIMARY section is ISSF Pistol", StringComparison.OrdinalIgnoreCase))
                return SectionTags.ISSF;
            if (csv.Headers[i].Contains("This form is for pistol shooters whose PRIMARY section is CAS", StringComparison.OrdinalIgnoreCase))
                return SectionTags.CAS;
            if (csv.Headers[i].Contains("This form is for pistol shooters whose PRIMARY section is Multi-Gun", StringComparison.OrdinalIgnoreCase))
                return SectionTags.MultiGun;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Speed Steel", StringComparison.OrdinalIgnoreCase))
                return SectionTags.SpeedSteel;
            if (csv.Headers[i].Contains("TThis form is for members whose PRIMARY section is Archery", StringComparison.OrdinalIgnoreCase))
                return SectionTags.Archery;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Air Rifle", StringComparison.OrdinalIgnoreCase))
                return SectionTags.AirRifle;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Black Powder", StringComparison.OrdinalIgnoreCase))
                return SectionTags.BlackPowder;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Service Rifle", StringComparison.OrdinalIgnoreCase))
                return SectionTags.ServiceRifle;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Smallbore Rifle", StringComparison.OrdinalIgnoreCase))
                return SectionTags.SmallboreRifle;
            if (csv.Headers[i].Contains("This form is for members whose PRIMARY section is Sporting Rifle", StringComparison.OrdinalIgnoreCase))
                return SectionTags.SportingRifle;
        }
        return SectionTags.None;
    }

    private int FindPNZNumber(CSVReader csv)
    {
        for (int i = 0; i < csv.Headers.Length; i++)
        {
            if (csv.Headers[i].Contains("Enter your PNZ membership Number", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private int FindFALNumber(CSVReader csv)
    {
        for (int i = 0; i < csv.Headers.Length; i++)
        {
            if (csv.Headers[i].Contains("Enter number (One alpha letter followed by seven numerical digits)", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }
}
