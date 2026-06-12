using Newtonsoft.Json.Linq;

namespace Frends.Odf.ReadSpreadsheet.Definitions;

/// <summary>
/// Result of the task.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates if the task completed successfully.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// A JSON array containing one object per extracted spreadsheet row.
    /// </summary>
    /// <example>[ { "Name": "John" }, { "Name": "Jane" } ]</example>
    public dynamic Data { get; set; }

    /// <summary>
    /// Error that occurred during task execution.
    /// </summary>
    /// <example>object { string Message, Exception AdditionalInfo }</example>
    public Error Error { get; set; }
}