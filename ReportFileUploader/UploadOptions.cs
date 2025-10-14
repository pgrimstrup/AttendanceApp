using System.Collections.Generic;

public sealed class UploadOptions
{
    public string FilePath { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    public bool DeleteAfterUpload { get; set; } = true;
    public string FormFieldName { get; set; } = "file";
    public Dictionary<string, string>? AdditionalHeaders { get; set; }
}
