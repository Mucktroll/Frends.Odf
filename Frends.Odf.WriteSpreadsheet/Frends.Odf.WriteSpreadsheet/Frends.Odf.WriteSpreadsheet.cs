using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Frends.Odf.WriteSpreadsheet.Definitions;
using Frends.Odf.WriteSpreadsheet.Helpers;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.WriteSpreadsheet;

/// <summary>
/// Task Class for Odf operations.
/// </summary>
public static class Odf
{
    private static readonly XNamespace OfficeNamespace = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";
    private static readonly XNamespace TableNamespace = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";
    private static readonly XNamespace TextNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    /// <summary>
    /// Generate an OpenDocument Spreadsheet (.ods) file by injecting user inputted JSON data into a built-in template.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Odf-WriteSpreadsheet)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string FilePath, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result WriteSpreadsheet(
        [PropertyTab] Input input,
        [PropertyTab] Options options,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidationHandler.Run(input, options);

            var normalizedPath = Path.GetFullPath(input.FilePath);
            var directory = Path.GetDirectoryName(normalizedPath);

            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentException("Invalid destination path.");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Destination directory not found: {directory}");

            if (!Path.GetExtension(normalizedPath).Equals(".ods", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The destination file must have a .ods extension.");

            if (File.Exists(normalizedPath))
            {
                if (input.ActionOnExistingFile == ActionOnExistingFile.Throw)
                    throw new IOException($"File already exists: {normalizedPath}");
            }

            if (input.Payload.Type != JTokenType.Array)
                throw new ArgumentException("The provided JSON payload must be a valid array of objects.");

            var jsonArray = (JArray)input.Payload;

            var assembly = typeof(Odf).Assembly;

            using var templateStream = assembly.GetManifestResourceStream("Frends.Odf.WriteSpreadsheet.Resources.template.ods") ?? throw new Exception("Could not find the embedded .ods template.");

            using var memoryStream = new MemoryStream();
            templateStream.CopyTo(memoryStream);

            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                var contentXml = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the embedded .ods template.");

                XDocument xDocument;

                using (var stream = contentXml.Open())
                {
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                    };

                    using var xmlReader = XmlReader.Create(stream, settings);
                    xDocument = XDocument.Load(xmlReader);
                }

                var firstTable = xDocument.Descendants(TableNamespace + "table").FirstOrDefault() ?? throw new Exception("No spreadsheet table found.");

                OdfSpreadsheetWriter.InjectData(firstTable, jsonArray, options.IncludeHeaderRow, TableNamespace, TextNamespace, cancellationToken);

                contentXml.Delete();
                var newContentXmlEntry = archive.CreateEntry("content.xml");

                using var newStream = newContentXmlEntry.Open();

                var newSettings = new XmlWriterSettings
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = false,
                };

                using var xmlWriter = XmlWriter.Create(newStream, newSettings);
                xDocument.Save(xmlWriter);
            }

            memoryStream.Position = 0;

            var tempFilePath = normalizedPath + ".tmp";

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.CopyTo(fileStream);
                }

                File.Move(tempFilePath, normalizedPath, true);
            }
            catch
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                throw;
            }

            return new Result
            {
                Success = true,
                FilePath = normalizedPath,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}