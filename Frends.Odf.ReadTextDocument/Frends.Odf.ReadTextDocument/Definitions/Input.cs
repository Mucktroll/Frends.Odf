using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Odf.ReadTextDocument.Definitions;

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// Full path of the target .odt file to be read.
    /// </summary>
    /// <example>c:\temp\foo.odt</example>
    [DefaultValue("")]
    [Required]
    public string FilePath { get; set; } = string.Empty;
}
