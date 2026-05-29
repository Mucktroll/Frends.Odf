using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Frends.Odf.ReadSpreadsheet.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    [Test]
    public void Should_Extract_File_Data()
    {
        // Tests the main data extraction functionality using default definition parameters and the test ODF XML framework generated in TestBase.cs.
        var result = Odf.ReadSpreadsheet(DefaultInput(), DefaultOptions(), CancellationToken.None);

        Assert.IsTrue(result.Success);

        var jsonArray = (JArray)result.Data;

        // Compares the extracted data from the Task with the expected output.
        Assert.AreEqual(1, jsonArray.Count);
        Assert.AreEqual("Value Test 1", jsonArray[0]["Key Test 1"].ToString());
        Assert.AreEqual("Value Test 2", jsonArray[0]["Key Test 2"].ToString());
    }

    [Test]
    public void Should_Use_Headers_When_ContainsHeaderRow_Is_True()
    {
        // Sets ContainsHeaderRow parameter to true to treat the first row as headers.
        var options = DefaultOptions();
        options.ContainsHeaderRow = true;

        // Extracts content.xml data.
        var result = Odf.ReadSpreadsheet(DefaultInput(), options, CancellationToken.None);

        Assert.IsTrue(result.Success);

        var jsonArray = (JArray)result.Data;

        Assert.AreEqual(1, jsonArray.Count);

        // Task should correctly map row 1 as keys and row 2 as values.
        Assert.AreEqual("Value Test 1", jsonArray[0]["Key Test 1"].ToString());
        Assert.AreEqual("Value Test 2", jsonArray[0]["Key Test 2"].ToString());
    }

    [Test]
    public void Should_Use_Generated_Headers_When_ContainsHeaderRow_Is_False()
    {
        // Sets ContainsHeaderRow parameter to false to treat the first row as values and use generated header names.
        var options = DefaultOptions();
        options.ContainsHeaderRow = false;

        // Extracts content.xml data.
        var result = Odf.ReadSpreadsheet(DefaultInput(), options, CancellationToken.None);

        Assert.IsTrue(result.Success);

        var jsonArray = (JArray)result.Data;

        Assert.AreEqual(2, jsonArray.Count);

        // Task should correctly map rows to the generated header names.
        Assert.AreEqual("Key Test 1", jsonArray[0]["Column_1"].ToString());
        Assert.AreEqual("Key Test 2", jsonArray[0]["Column_2"].ToString());
        Assert.AreEqual("Value Test 1", jsonArray[1]["Column_1"].ToString());
        Assert.AreEqual("Value Test 2", jsonArray[1]["Column_2"].ToString());
    }

    [Test]
    public void Should_Throw_When_ContentXml_Is_Missing()
    {
        // Creates a temporary .ods zip file without injecting content.xml.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ods");
        ZipFile.Open(path, ZipArchiveMode.Create).Dispose();

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Tries to extract content.xml data.
            var exception = Assert.Throws<Exception>(() => Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None));

            // Confirms exception message matches the expected message when content.xml is missing.
            Assert.That(exception.Message, Contains.Substring("content.xml is missing"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Return_Empty_Data()
    {
        // Empty XML spreadsheet framework with no rows or cells.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table table:name=""Sheet1"">
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        // Path of the created .ods file injected with the empty XML data.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts content.xml data.
            var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

            // Checks that the Task returned successfully and that the extracted data is an empty string.
            Assert.IsTrue(result.Success);

            var jsonArray = (JArray)result.Data;
            Assert.AreEqual(0, jsonArray.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Return_Failure_With_Corrupt_File_Input()
    {
        // Creates a temporary .ods file with standard text instead of a valid Zip archive.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ods");
        File.WriteAllText(path, "This is corrupt file input.");

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            var options = DefaultOptions();
            options.ThrowErrorOnFailure = false;

            // Extracts the corrupt file input.
            var result = Odf.ReadSpreadsheet(input, options, CancellationToken.None);

            // Checks that the Task correctly handled the corrupt file and returned Success = false.
            Assert.IsFalse(result.Success);
            Assert.IsNotNull(result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Handle_Repeated_Columns()
    {
        // XML framework with 'table:number-columns-repeated' attribute.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table>
                            <table:table-row>
                                <table:table-cell><text:p>Value Test 1</text:p></table:table-cell>
                                <table:table-cell table:number-columns-repeated=""2""/>
                                <table:table-cell><text:p>Value Test 4</text:p></table:table-cell>
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Set ContainsHeaderRow = false to treat the single row as data.
            var options = DefaultOptions();
            options.ContainsHeaderRow = false;

            // Extracts content.xml data.
            var result = Odf.ReadSpreadsheet(input, options, CancellationToken.None);
            Assert.IsTrue(result.Success);

            var jsonArray = (JArray)result.Data;
            var firstRow = jsonArray[0];

            // Checks that the Task created a single row with 4 columns: Value Test 1, [Empty], [Empty], Value Test 4.
            Assert.AreEqual("Value Test 1", firstRow["Column_1"].ToString());
            Assert.AreEqual(string.Empty, firstRow["Column_2"].ToString());
            Assert.AreEqual(string.Empty, firstRow["Column_3"].ToString());
            Assert.AreEqual("Value Test 4", firstRow["Column_4"].ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Handle_Duplicate_Headers()
    {
        // XML framework with header row containing duplicates.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table>
                            <table:table-row>
                                <table:table-cell><text:p>Key Test</text:p></table:table-cell>
                                <table:table-cell><text:p>Key Test</text:p></table:table-cell>
                                <table:table-cell><text:p>Key Test</text:p></table:table-cell>
                            </table:table-row>
                            <table:table-row>
                                <table:table-cell><text:p>Value Test</text:p></table:table-cell>
                                <table:table-cell><text:p>Value Test</text:p></table:table-cell>
                                <table:table-cell><text:p>Value Test</text:p></table:table-cell>
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        // Path of the created .ods file injected with the duplicate header XML data.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts content.xml data.
            var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);
            Assert.IsTrue(result.Success);

            var jsonArray = (JArray)result.Data;
            var dataRow = jsonArray[0];

            // Checks that the Task correctly appended unique ID values to duplicates.
            Assert.AreEqual("Value Test", dataRow["Key Test"].ToString());
            Assert.AreEqual("Value Test", dataRow["Key Test_1"].ToString());
            Assert.AreEqual("Value Test", dataRow["Key Test_2"].ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Handle_Unicode_Content()
    {
        // XML framework with Scandinavian characters.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table>
                            <table:table-row>
                                <table:table-cell><text:p>AäÄaOöÖo</text:p></table:table-cell>
                            </table:table-row>
                            <table:table-row>
                                <table:table-cell><text:p>ÖöÄä</text:p></table:table-cell>
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        // Path of the created .ods file injected with the Scandinavian XML data.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts content.xml data.
            var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

            Assert.IsTrue(result.Success);

            var jsonArray = (JArray)result.Data;

            // Checks that the Task returned successfully and that the extracted data matches the known input.
            Assert.AreEqual("ÖöÄä", jsonArray[0]["AäÄaOöÖo"].ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Handle_Malformed_Xml()
    {
        // XML framework missing a closing <text:p> tag.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table>
                            <table:table-row>
                                <table:table-cell><text:p>This tag never closes.
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        // Path of the created .ods file injected with the malformed XML data.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts the malformed XML input.
            var exception = Assert.Throws<Exception>(() => Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None));

            Assert.IsNotNull(exception);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Handle_Odf_Elements()
    {
        // XML framework with ODF tabs, multiple whitespace, and line break tags.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table>
                            <table:table-row>
                                <table:table-cell><text:p>Key Test 1</text:p></table:table-cell>
                                <table:table-cell><text:p>Key Test 2</text:p></table:table-cell>
                            </table:table-row>
                            <table:table-row>
                                <table:table-cell><text:p>Value<text:tab/>Test 1.</text:p></table:table-cell>
                                <table:table-cell><text:p>Value<text:s text:c=""4""/> Test<text:line-break/> 2.</text:p></table:table-cell>
                            </table:table-row>
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        // Path of the created .ods file injected with the ODF elements.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts the ODF elements input.
            var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

            Assert.IsTrue(result.Success);

            var jsonArray = (JArray)result.Data;

            // Checks that the Task returned successfully and that the extracted content matches the known input.
            Assert.AreEqual("Value\tTest 1.", jsonArray[0]["Key Test 1"].ToString());
            Assert.AreEqual("Value     Test" + Environment.NewLine + " 2.", jsonArray[0]["Key Test 2"].ToString());
        }
        finally
        {
            File.Delete(path);
        }
    }
}