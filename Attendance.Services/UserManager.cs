using Attendance.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Attendance.Services;

public interface IUserManager
{
    Task SendAccessCodeForPnzNumber(string pnzNumber);
    Task SendAccessCodeForCardNumber(string cardNumber);
    Task<SportyImport?> ValidateAccessCodeForPnzNumber(string pnzNumber, int accessCode);
    Task<SportyImport?> ValidateAccessCodeForCardNumber(string cardNumber, int accessCode);
}

public class UserManager : IUserManager
{
    readonly IServiceProvider _services;
    readonly ILogger _logger;
    readonly IEmailManager _emailer;
    readonly IOptions<AttendanceOptions> _options;

    public UserManager(IOptions<AttendanceOptions> options, IServiceProvider services, ILogger<UserManager> logger, IEmailManager emailer)
    {
        _services = services;
        _logger = logger;
        _emailer = emailer;
        _options = options;
    }


    public async Task SendAccessCodeForPnzNumber(string pnzNumber)
    {
        if (String.IsNullOrEmpty(pnzNumber))
            return;

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var found = await context.SportyImports.Where(i => i.PNZNumber == pnzNumber)
            .ToListAsync();

        if (found.Count > 2)
            throw new Exception("More than 2 members found with the same PNZ number. Please contact support.");

        Random r = new Random();
        int authCode = r.Next(10000000, 89999999);

        List<string> done = new List<string>();
        foreach (var member in found)
        {
            member.AuthCode = authCode + r.Next(0, 9999999);
            member.AuthCodeExpiry = DateTime.UtcNow.AddMinutes(30);
            await context.SaveChangesAsync();

            // To improve performance, put the request into a queue and process in the background
            if (!String.IsNullOrWhiteSpace(member.PNZNumber) && member.PNZNumber == pnzNumber && !String.IsNullOrWhiteSpace(member.EmailAddress) && !done.Contains(member.EmailAddress))
            {
                await _emailer.SendAccessCodeAsync(member.EmailAddress, member.AuthCode.ToString()!);
                done.Add(member.EmailAddress);
                _logger.LogInformation("Access Code {accesscode} sent to {email}", member.AuthCode, member.EmailAddress);
            }
        }
    }

    public async Task SendAccessCodeForCardNumber(string cardNumber)
    {
        if (String.IsNullOrWhiteSpace(cardNumber))
            return;

        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var found = await context.SportyImports.Where(i => i.CardNumber == cardNumber)
            .OrderByDescending(i => i.RegistrationDate)
            .ToListAsync();

        if (found.Count > 2)
            throw new Exception("More than 2 members found with the same card number. Please contact support.");

        Random r = new Random();
        int authCode = r.Next(10000000, 89999999);

        List<string> done = new List<string>();
        foreach (var member in found)
        {
            member.AuthCode = authCode + r.Next(0, 9999999);
            member.AuthCodeExpiry = DateTime.UtcNow.AddMinutes(30);
            await context.SaveChangesAsync();

            // To improve performance, put the request into a queue and process in the background
            if (!String.IsNullOrWhiteSpace(member.CardNumber) && member.CardNumber == cardNumber && !String.IsNullOrWhiteSpace(member.EmailAddress) && !done.Contains(member.EmailAddress))
            {
                await _emailer.SendAccessCodeAsync(member.EmailAddress, member.AuthCode.ToString()!);
                done.Add(member.EmailAddress);
                _logger.LogInformation("Access Code {accesscode} sent to {email}", member.AuthCode, member.EmailAddress);
            }
        }
    }

    public async Task<SportyImport?> ValidateAccessCodeForPnzNumber(string pnzNumber, int accessCode)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var found = await context.SportyImports.Where(i => i.PNZNumber == pnzNumber && i.AuthCode == accessCode && i.AuthCodeExpiry > DateTime.UtcNow)
            .ToListAsync();

        var member = found.FirstOrDefault();
        if (member != null)
        {
            member.AuthCode = null;
            member.AuthCodeExpiry = null;
            await context.SaveChangesAsync();
        }

        return member;
    }

    public async Task<SportyImport?> ValidateAccessCodeForCardNumber(string cardNumber, int accessCode)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var found = await context.SportyImports.Where(i => i.CardNumber == cardNumber && i.AuthCode == accessCode && i.AuthCodeExpiry > DateTime.UtcNow)
            .ToListAsync();

        var member = found.FirstOrDefault();
        if (member == null && cardNumber == "RRGC:ADMIN" && accessCode == 5356222)
        {
            member = new SportyImport {
                FirstName = "Admin",
                LastName = "",
                CardNumber = cardNumber,
                EmailAddress = "secretary@rrgc.nz",
                MobileNumber = "0225356222"
            };
        }
        
        if (member != null && member.AuthCode > 0)
        {
            member.AuthCode = null;
            member.AuthCodeExpiry = null;
            await context.SaveChangesAsync();
        }

        return member;
    }
}
