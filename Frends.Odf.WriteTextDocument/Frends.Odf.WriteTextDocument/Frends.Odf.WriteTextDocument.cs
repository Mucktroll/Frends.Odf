using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Frends.Odf.WriteTextDocument.Definitions;
using Frends.Odf.WriteTextDocument.Helpers;
using Newtonsoft.Json.Linq;

namespace Frends.Odf.WriteTextDocument;

/// <summary>
/// Task Class for Odf operations.
/// </summary>
public static class Odf
{
    private static readonly XNamespace TextNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";
    private static readonly XNamespace OfficeNamespace = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

    /// <summary>
    /// Generate an OpenDocument Text (.odt) file by injecting user inputted JSON data
    /// into a built-in template.
    /// Each JSON property is written as a separate paragraph in the format "key: value".
    /// For example, { "Name": "John" } produces a paragraph containing "Name: John".
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Odf-WriteTextDocument)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string FilePath, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result WriteTextDocument(
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

            if (!Path.GetExtension(normalizedPath).Equals(".odt", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The destination file must have a .odt extension.");

            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Destination directory not found: {directory}");

            if (File.Exists(normalizedPath))
            {
                if (input.ActionOnExistingFile == ActionOnExistingFile.Throw)
                    throw new IOException($"File already exists: {normalizedPath}");
            }

            if (input.Payload.Type != JTokenType.Array)
                throw new ArgumentException("The provided JSON payload must be a valid array of objects.");

            var jsonArray = (JArray)input.Payload;

            var assembly = typeof(Odf).Assembly;

            using var templateStream = assembly.GetManifestResourceStream("Frends.Odf.WriteTextDocument.Resources.template.odt") ?? throw new Exception("Could not find the embedded .odt template.");

            using var memoryStream = new MemoryStream();
            templateStream.CopyTo(memoryStream);

            // Open the writable stream in Update mode. leaveOpen: true prevents destroying the MemoryStream upon exit.
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, leaveOpen: true))
            {
                var contentXml = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the embedded .odt template.");

                XDocument xDocument;

                using (var stream = contentXml.Open())
                {
                    // Configure XmlReader to disable DTDs and external entities (XXE protection).
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null,
                    };

                    using var xmlReader = XmlReader.Create(stream, settings);
                    xDocument = XDocument.Load(xmlReader);
                }

                var textBody = xDocument.Root?.Element(OfficeNamespace + "body")?.Element(OfficeNamespace + "text") ?? throw new Exception("Invalid template structure.");

                foreach (var obj in jsonArray)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (obj is not JObject jObject)
                        throw new ArgumentException("The JSON payload must contain valid JSON objects.");

                    foreach (var property in jObject.Properties())
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var key = property.Name;
                        var value = string.Empty;

                        if (property.Value.Type != JTokenType.Null)
                            value = property.Value.ToString();

                        var paragraphContent = $"{key}: {value}";

                        // Wrap in a standard <text:p> XML tag and inject them into textBody, updating xDocument.
                        var paragraphElement = new XElement(TextNamespace + "p", paragraphContent);
                        textBody.Add(paragraphElement);
                    }
                }

                // ZipArchive cannot edit an existing file. Delete previous content.xml and create a new one.
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

            var tempFilePath = Path.Combine(directory, $"{Path.GetFileName(normalizedPath)}.{Guid.NewGuid():N}.tmp");

            try
            {
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    memoryStream.CopyTo(fileStream);
                }

                var overwrite = input.ActionOnExistingFile != ActionOnExistingFile.Throw;

                try
                {
                    File.Move(tempFilePath, normalizedPath, overwrite);
                }
                catch (IOException ex) when (!overwrite)
                {
                    throw new IOException($"File already exists: {normalizedPath}", ex);
                }
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