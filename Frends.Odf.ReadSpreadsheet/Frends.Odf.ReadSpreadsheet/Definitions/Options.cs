using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Odf.ReadSpreadsheet.Definitions;

/// <summary>
/// Additional parameters.
/// </summary>
public class Options
{
    /// <summary>
    /// If true, the Task will recognise the first row of the .ods file as headers.
    /// If false, the Task will treat the first row of the .ods file the same as all other rows.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ContainsHeaderRow { get; set; } = true;

    /// <summary>
    /// Whether to throw an error on failure.
    /// </summary>
    /// <example>true</example>
    [DefaultValue(true)]
    public bool ThrowErrorOnFailure { get; set; } = true;

    /// <summary>
    /// Overrides the error message on failure.
    /// </summary>
    /// <example>Custom error message</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ErrorMessageOnFailure { get; set; } = string.Empty;
}
