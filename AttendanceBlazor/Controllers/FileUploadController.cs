using Microsoft.AspNetCore.Mvc;
using Attendance.Services;

namespace AttendanceBlazor.Controllers;

[ApiController]
[Route("api/upload")]
public class FileUploadController : ControllerBase
{
    private readonly IEntraPassImporter _entrapassImporter;
    private readonly ISportyRegistrationImporter _sportyImporter;

    public FileUploadController(IEntraPassImporter entrapassimporter, ISportyRegistrationImporter sportyImporter)
    {
        _entrapassImporter = entrapassimporter;
        _sportyImporter = sportyImporter;
    }

    [HttpGet("entrapass")]
    public IActionResult GetEntrapass()
    {
        return StatusCode(405, "GET method is not allowed. Please use POST to upload a file.");
    }

    [HttpGet("sporty")]
    public IActionResult GetSporty()
    {
        return StatusCode(405, "GET method is not allowed. Please use POST to upload a file.");
    }

    [HttpPost("entrapass")]
    public async Task<IActionResult> PostEntrapassAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (csvFile.ContentType != "text/csv")
        {
            return BadRequest("Invalid file type. Only CSV files are allowed.");
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem);
            mem.Position = 0;

            var result = await _entrapassImporter.Import(mem);
            if (result)
            {
                return Ok(new { Message = "File uploaded and processed successfully." });
            }
            else
            {
                return BadRequest("File could not be processed. Ensure it is in the expected format.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpPost("sporty")]
    public async Task<IActionResult> PostSportyAsync(IFormFile csvFile)
    {
        if (csvFile == null || csvFile.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (csvFile.ContentType != "text/csv")
        {
            return BadRequest("Invalid file type. Only CSV files are allowed.");
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem);
            mem.Position = 0;

            var result = await _sportyImporter.Import(mem);
            if (result)
            {
                return Ok(new { Message = "File uploaded and processed successfully." });
            }
            else
            {
                return BadRequest("File could not be processed. Ensure it is in the expected format.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}