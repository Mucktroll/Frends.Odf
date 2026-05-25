using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Odf.ReadSpreadsheet.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Full path of the target .ods file to be read.
    /// </summary>
    /// <example>c:\temp\foo.ods</example>
    [DefaultValue("")]
    [Required]
    public string FilePath { get; set; } = string.Empty;
}
