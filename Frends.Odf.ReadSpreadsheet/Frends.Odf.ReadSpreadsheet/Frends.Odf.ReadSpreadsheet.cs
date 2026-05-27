using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Frends.Odf.ReadSpreadsheet.Definitions;
using Frends.Odf.ReadSpreadsheet.Helpers;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.ReadSpreadsheet;

/// <summary>
/// Task Class for reading Odf spreadsheets.
/// </summary>
public static class Odf
{
    /// <summary>
    /// Receive an OpenDocument Spreadsheet (.ods) file and extract all tabular data, converting it into JSON format.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Odf-ReadSpreadsheet)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, JToken Data, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result ReadSpreadsheet(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, options);

            // Checks if input file exists.
            if (!File.Exists(input.FilePath))
                throw new FileNotFoundException($"Input file not found: {input.FilePath}");

            // Checks if input file matches the expected ODF file type.
            if (!Path.GetExtension(input.FilePath).Equals(".ods", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The input file is not in .ods format.");

            // Normalises the path to prevent path traversal.
            var normalizedPath = Path.GetFullPath(input.FilePath);

            // Open the zip archive.
            using ZipArchive archive = ZipFile.OpenRead(normalizedPath);

            var contentXml = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the .ods file.");

            // Check the unzipped file size is below 50MB to prevent zip bombing.
            if (contentXml.Length > 50 * 1024 * 1024)
                throw new Exception("content.xml is larger than the maximum allowed file size of 50MB.");

            // Cancellation token should be provided to methods that support it
            // and checked during long-running operations, e.g., loops.
            cancellationToken.ThrowIfCancellationRequested();

            using var stream = contentXml.Open();

            // Configure XmlReader to disable DTDs and external entities.
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            using var xmlReader = XmlReader.Create(stream, settings);
            var xDocument = XDocument.Load(xmlReader);

            // Define the standard OpenDocument namespaces for spreadsheets.
            XNamespace tableNs = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
            XNamespace textNs = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

            // Select the first sheet in the spreadsheet.
            var firstTable = xDocument.Descendants(tableNs + "table").FirstOrDefault();

            // If the first spreadsheet is null, return an empty JArray.
            if (firstTable == null)
            {
                return new Result
                {
                    Success = true,
                    Data = new JArray(),
                    Error = null,
                };
            }

            // Use the helper class to extract the data and map the JSON.
            var jsonArray = OdfSpreadsheetParser.ExtractData(firstTable, tableNs, textNs, options.ContainsHeaderRow, cancellationToken);

            return new Result
            {
                Success = true,
                Data = jsonArray,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}