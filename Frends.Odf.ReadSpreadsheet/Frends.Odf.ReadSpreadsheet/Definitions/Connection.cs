using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Frends.Odf.ReadSpreadsheet.Definitions;

/// <summary>
/// Connection parameters.
/// </summary>
// TODO: Remove this class if the task does not make connections
public class Connection
{
    /// <summary>
    /// Connection string to the target service (e.g., database, API endpoint).
    /// </summary>
    /// <example>Host=127.0.0.1;Port=5432</example>
    [DisplayFormat(DataFormatString = "Text")]
    [DefaultValue("")]
    public string ConnectionString { get; set; } = string.Empty;
}
