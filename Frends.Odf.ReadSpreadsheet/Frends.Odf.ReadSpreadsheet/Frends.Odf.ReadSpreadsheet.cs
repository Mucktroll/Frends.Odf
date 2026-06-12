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
    private static readonly XNamespace TableNamespace = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
    private static readonly XNamespace TextNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    /// <summary>
    /// Receive an OpenDocument Spreadsheet (.ods) file and extract tabular data from the first sheet, converting it into JSON format.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Odf-ReadSpreadsheet)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, dynamic Data, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result ReadSpreadsheet(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, options);

            if (!Path.GetExtension(input.FilePath).Equals(".ods", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The input file is not in .ods format.");

            if (!File.Exists(input.FilePath))
                throw new FileNotFoundException($"Input file not found: {input.FilePath}");

            var normalizedPath = Path.GetFullPath(input.FilePath);

            using ZipArchive archive = ZipFile.OpenRead(normalizedPath);

            var contentXml = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the .ods file.");

            // Check the unzipped file size is below 50MB to prevent zip bombing.
            if (contentXml.Length > 50 * 1024 * 1024)
                throw new Exception("content.xml is larger than the maximum allowed file size of 50MB.");

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

            var firstTable = xDocument.Descendants(TableNamespace + "table").FirstOrDefault();

            if (firstTable == null)
            {
                return new Result
                {
                    Success = true,
                    Data = new JArray(),
                    Error = null,
                };
            }

            var jsonArray = OdfSpreadsheetParser.ExtractData(firstTable, TableNamespace, TextNamespace, options.ContainsHeaderRow, cancellationToken);

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