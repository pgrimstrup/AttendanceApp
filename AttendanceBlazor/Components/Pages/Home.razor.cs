namespace AttendanceBlazor.Components.Pages;

public partial class Home
{
    readonly ILogger _logger;

    public Home(ILogger<Home> logger)
    {
        _logger = logger;
    }
}
