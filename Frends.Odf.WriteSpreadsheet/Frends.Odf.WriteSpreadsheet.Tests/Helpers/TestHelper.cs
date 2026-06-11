using System;
using System.IO.Compression;
using System.Xml.Linq;

namespace Frends.Odf.WriteSpreadsheet.Tests.Helpers;

internal class TestHelper
{
    /// <summary>
    /// Reads and returns content.xml from a generated .ods file as a string for testing.
    /// </summary>
    /// <param name="filePath">The path to the generated .ods file.</param>
    /// <returns>content.xml content as a string.</returns>
    internal static string ReadOdsContent(string filePath)
    {
        using var archive = ZipFile.OpenRead(filePath);
        var contentXmlEntry = archive.GetEntry("content.xml") ?? throw new Exception("content.xml is missing from the generated file.");

        using var stream = contentXmlEntry.Open();
        var xDocument = XDocument.Load(stream);

        return xDocument.ToString();
    }
}