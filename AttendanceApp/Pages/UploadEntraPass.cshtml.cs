using Attendance.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Attendance.Pages
{
    public class UploadEntraPassModel : PageModel
    {
        IEntraPassImporter _importer;

        [BindProperty]
        public IFormFile CsvFile { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public bool? Success { get; set; }

        public UploadEntraPassModel(IEntraPassImporter importer)
        {
            _importer = importer;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync([FromQuery] bool interactive = true)
        {
            if (CsvFile == null || CsvFile.Length == 0)
            {
                if (interactive)
                {
                    ModelState.AddModelError(string.Empty, "Please upload a valid CSV file.");
                    return Page();
                }
                else
                {
                    return BadRequest("No file uploaded.");
                }
            }

            var success = await _importer.Import(CsvFile.OpenReadStream());

            if (interactive)
                return RedirectToPage(new { Success = success });
            else
                return StatusCode(success ? 200 : 400);
        }
    }
}
