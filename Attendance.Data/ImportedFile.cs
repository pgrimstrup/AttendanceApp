using System;
using System.Collections.Generic;
using System.Text;

namespace Attendance.Data;

public class ImportedFile
{
    public int Id { get; set; }
    public DateTimeOffset DateImported { get; set; }
    public string FileType { get; set; }= string.Empty;
    public string FileContent { get; set; }= string.Empty;
}
