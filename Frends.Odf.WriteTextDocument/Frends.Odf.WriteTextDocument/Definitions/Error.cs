using System;

namespace Frends.Odf.WriteTextDocument.Definitions;

/// <summary>
/// Error that occurred during the task.
/// </summary>
public class Error
{
    /// <summary>
    /// Summary of the error.
    /// </summary>
    /// <example>File already exists: C:\temp\fake_path.odt</example>
    public string Message { get; set; }

    /// <summary>
    /// Additional information about the error.
    /// </summary>
    /// <example>System.IO.IOException</example>
    public Exception AdditionalInfo { get; set; }
}