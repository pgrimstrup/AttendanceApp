using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Attendance.Pages
{
    public class EmbedModel : PageModel
    {
        private readonly ILogger<EmbedModel> _logger;

        public EmbedModel(ILogger<EmbedModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
