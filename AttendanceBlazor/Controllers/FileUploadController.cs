using Microsoft.AspNetCore.Mvc;
using Attendance.Services;

namespace AttendanceBlazor.Controllers
{
    [ApiController]
    [Route("api/upload")]
    public class FileUploadController : ControllerBase
    {
        private readonly IEntraPassImporter _importer;

        public FileUploadController(IEntraPassImporter importer)
        {
            _importer = importer;
        }

        [HttpGet("entrapass")]
        public IActionResult Get()
        {
            return StatusCode(405, "GET method is not allowed. Please use POST to upload a file.");
        }

        [HttpPost("entrapass")]
        public async Task<IActionResult> PostAsync(IFormFile csvFile)
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

                var result = await _importer.Import(mem);
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
}