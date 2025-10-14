using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public sealed class FileUploader
{
    private readonly HttpClient _http;
    private readonly ILogger<FileUploader> _logger;
    private UploadOptions _options;

    public FileUploader(HttpClient http, ILogger<FileUploader> logger, IOptionsMonitor<UploadOptions> options)
    {
        _http = http;
        _logger = logger;
        _options = options.CurrentValue;

        options.OnChange(o => _options = o);
    }

    public async Task TryUploadAsync(CancellationToken ct)
    {
        var filePath = _options.FilePath;
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(_options.EndpointUrl))
        {
            _logger.LogWarning("Configuration invalid: FilePath or EndpointUrl is blank.");
            return;
        }

        var folder = Path.GetDirectoryName(filePath);
        var pattern = Path.GetFileName(filePath);
        if (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(pattern))
        {
            _logger.LogWarning("Configuration invalid: Folder in FilePath or FileName is blank.");
            return;
        }

        // Find all files matching the pattern
        foreach (var path in Directory.GetFiles(folder, pattern))
        {
            _logger.LogInformation("Found file. Preparing upload: {Path}", path);

            // Prepare request with optional API key and custom headers
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.EndpointUrl);

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                // Common pattern; adjust if your API expects something else
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            }

            if (_options.AdditionalHeaders is not null)
            {
                foreach (var kv in _options.AdditionalHeaders)
                {
                    request.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
                }
            }

            // Send as multipart/form-data (typical for file uploads)
            using var form = new MultipartFormDataContent();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            var content = new StreamContent(fs);
            // Optional: set content type based on your file
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

            var fieldName = string.IsNullOrWhiteSpace(_options.FormFieldName) ? "file" : _options.FormFieldName;
            form.Add(content, fieldName, Path.GetFileName(path));

            request.Content = form;

            // Retry (simple exponential backoff: 3 tries)
            const int maxAttempts = 3;
            var delay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Upload succeeded (HTTP {Status}).", (int)response.StatusCode);

                        if (_options.DeleteAfterUpload)
                        {
                            try
                            {
                                fs.Dispose(); // ensure file handle is closed before delete
                                File.Delete(path);
                                _logger.LogInformation("Deleted file after successful upload: {Path}", path);
                            }
                            catch (Exception delEx)
                            {
                                _logger.LogWarning(delEx, "Uploaded, but failed to delete file: {Path}", path);
                            }
                        }
                        return;
                    }

                    var body = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogWarning("Upload failed (HTTP {Status}) attempt {Attempt}/{Max}. Body: {Body}",
                        (int)response.StatusCode, attempt, maxAttempts, Truncate(body, 500));
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    _logger.LogInformation("Upload attempt canceled due to service stop.");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Upload exception attempt {Attempt}/{Max}.", attempt, maxAttempts);
                }

                if (attempt < maxAttempts)
                {
                    await Task.Delay(delay, ct);
                    delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
                }
            }

            _logger.LogError("All upload attempts failed for file: {Path}", path);
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s.Substring(0, max) + "…";
}
