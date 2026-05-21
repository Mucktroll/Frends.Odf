using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Frends.Odf.ReadTextDocument.Definitions;
using Frends.Odf.ReadTextDocument.Helpers;

namespace Frends.Odf.ReadTextDocument;

/// <summary>
/// Task Class for reading Odf text documents.
/// </summary>
public static class Odf
{
    /// <summary>
    /// Extracts readable text from OpenDocument Text (.odt) files.
    /// [Documentation](https://tasks.frends.com/tasks/frends-tasks/Frends-Odf-ReadTextDocument)
    /// </summary>
    /// <param name="input">Essential parameters.</param>
    /// <param name="options">Additional parameters.</param>
    /// <param name="cancellationToken">A cancellation token provided by Frends Platform.</param>
    /// <returns>object { bool Success, string Content, object Error { string Message, Exception AdditionalInfo } }</returns>
    public static Result ReadTextDocument(
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
            if (!Path.GetExtension(input.FilePath).Equals(".odt", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The input file is not in .odt format.");

            // Normalises the path to prevent path traversal.
            var normalizedPath = Path.GetFullPath(input.FilePath);

            // Open the zip archive.
            using ZipArchive archive = ZipFile.OpenRead(normalizedPath);

            var contentXml = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the .odt file.");

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

            // Define the standard OpenDocument text namespace.
            XNamespace textNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

            // Summary: Extract paragraphs and headings in document order.
            /*
             * Descendants() retrieves all XElements in the document.
             * Combines the ODF namespace with "p" or "h" to create <text:p> or <text:h> which is valid XName property.
             * Filters to get only the XElements that have the paragraphs and headings XName property.
             * Uses Ancestors() to prevent duplicate element value extraction.
             * Retrieves the text content inside each XElement using helper method.
            */
            var textElements = xDocument.Descendants()
                .Where(x => (x.Name == textNamespace + "p" || x.Name == textNamespace + "h")
                         && !x.Ancestors().Any(a => a.Name == textNamespace + "p" || a.Name == textNamespace + "h"))
                .Select(x => Helpers.OdfTextParser.ParseOdfElements(x, textNamespace));

            var extractedContent = string.Join(Environment.NewLine, textElements);

            return new Result
            {
                Success = true,
                Content = extractedContent,
                Error = null,
            };
        }
        catch (Exception ex)
        {
            return ex.Handle(options);
        }
    }
}