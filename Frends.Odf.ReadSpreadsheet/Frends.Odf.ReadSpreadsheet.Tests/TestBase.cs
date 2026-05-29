using System;
using System.IO;
using System.IO.Compression;
using Frends.Odf.ReadSpreadsheet.Definitions;
using NUnit.Framework;

namespace Frends.Odf.ReadSpreadsheet.Tests;

internal abstract class TestBase
{
    // Input file path variable available to all test classes.
    protected string ValidTestFilePath { get; private set; }

    [SetUp]
    public void SetupBase()
    {
        // Initialise a temporary path for the .ods file used in tests.
        ValidTestFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ods");

        // Create a zip archive at the temporary path.
        using var archive = ZipFile.Open(ValidTestFilePath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("content.xml");
        using var writer = new StreamWriter(entry.Open());

        // Inject a basic ODF Spreadsheet XML framework into the zip archive.
        writer.Write(@"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table table:name=""Sheet1"">
                            <table:table-row>
                                <table:table-cell><text:p>Key Test 1</text:p></table:table-cell>
                                <table:table-cell><text:p>Key Test 2</text:p></table:table-cell>
                            </table:table-row>
                            <table:table-row>
                                <table:table-cell><text:p>Value Test 1</text:p></table:table-cell>
                                <table:table-cell><text:p>Value Test 2</text:p></table:table-cell>
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>");
    }

    [TearDown]
    public void TearDownBase()
    {
        // Delete the temporary file after each test runs.
        if (File.Exists(ValidTestFilePath))
        {
            File.Delete(ValidTestFilePath);
        }
    }

    protected static Options DefaultOptions() => new()
    {
        ContainsHeaderRow = true,
    };

    protected Input DefaultInput() => new()
    {
        // Assigns the input file path variable to the default Input used in tests.
        FilePath = ValidTestFilePath,
    };
}