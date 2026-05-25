using System;

namespace Frends.Odf.ReadSpreadsheet.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>Input file not found: C:\temp\fake_path.ods</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>System.IO.FileNotFoundException</example>
    public Exception AdditionalInfo { get; set; }
}
