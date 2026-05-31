using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.WriteTextDocument.Definitions;

/// <summary>
/// Action to take if the destination file already exists.
/// </summary>
public enum ActionOnExistingFile
{
    /// <summary>
    /// Replaces the existing file.
    /// </summary>
    Overwrite,

    /// <summary>
    /// Throws an error if the file already exists.
    /// </summary>
    Throw,
}

/// <summary>
/// Essential parameters.
/// </summary>
public class Input
{
    /// <summary>
    /// JSON data input to be written into the .odt document.
    /// </summary>
    /// <example>[ { "Name": "John" }, { "Name": "Jane" } ]</example>
    [Required]
    [DefaultValue("")]
    public JToken Payload { get; set; }

    /// <summary>
    /// Full path of the destination for the new .odt file.
    /// </summary>
    /// <example>c:\temp\foo.odt</example>
    [Required]
    [DefaultValue("")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Defines how the file write should work if a file with the new name already exists.
    /// </summary>
    /// <example>ActionOnExistingFile.Throw</example>
    [DefaultValue(ActionOnExistingFile.Throw)]
    public ActionOnExistingFile ActionOnExistingFile { get; set; } = ActionOnExistingFile.Throw;
}