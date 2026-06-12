using System;
using System.Threading;
using Frends.Odf.ReadSpreadsheet.Tests.Helpers;
using NUnit.Framework;

namespace Frends.Odf.ReadSpreadsheet.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    [Test]
    public void Should_Extract_File_Data()
    {
        var result = Odf.ReadSpreadsheet(DefaultInput(), DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;

        Assert.That(jsonArray.Count, Is.EqualTo(1));
        Assert.That(jsonArray[0]["Key Test 1"]?.ToString(), Is.EqualTo("Value Test 1"));
        Assert.That(jsonArray[0]["Key Test 2"]?.ToString(), Is.EqualTo("Value Test 2"));
    }

    [Test]
    public void Should_Use_Headers_When_ContainsHeaderRow_Is_True()
    {
        var options = DefaultOptions();
        options.ContainsHeaderRow = true;

        var result = Odf.ReadSpreadsheet(DefaultInput(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;

        Assert.That(jsonArray.Count, Is.EqualTo(1));
        Assert.That(jsonArray[0]["Key Test 1"]?.ToString(), Is.EqualTo("Value Test 1"));
        Assert.That(jsonArray[0]["Key Test 2"]?.ToString(), Is.EqualTo("Value Test 2"));
    }

    [Test]
    public void Should_Use_Generated_Headers_When_ContainsHeaderRow_Is_False()
    {
        var options = DefaultOptions();
        options.ContainsHeaderRow = false;

        var result = Odf.ReadSpreadsheet(DefaultInput(), options, CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;

        Assert.That(jsonArray.Count, Is.EqualTo(2));
        Assert.That(jsonArray[0]["Column_1"]?.ToString(), Is.EqualTo("Key Test 1"));
        Assert.That(jsonArray[0]["Column_2"]?.ToString(), Is.EqualTo("Key Test 2"));
        Assert.That(jsonArray[1]["Column_1"]?.ToString(), Is.EqualTo("Value Test 1"));
        Assert.That(jsonArray[1]["Column_2"]?.ToString(), Is.EqualTo("Value Test 2"));
    }

    [Test]
    public void Should_Throw_When_ContentXml_Is_Missing()
    {
        using var testHelper = new TestHelper();
        var path = testHelper.CreateMissingContentXmlFile();

        var input = DefaultInput();
        input.FilePath = path;

        var exception = Assert.Throws<Exception>(() => Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(exception.Message, Contains.Substring("content.xml is missing"));
    }

    [Test]
    public void Should_Return_Empty_Data()
    {
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:table=""urn:oasis:names:tc:opendocument:xmlns:table:1.0"">
                <office:body>
                    <office:spreadsheet>
                        <table:table table:name=""Sheet1"">
                        </table:table>
                    </office:spreadsheet>
                </office:body>
            </office:document-content>";

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;
        Assert.That(jsonArray.Count, Is.EqualTo(0));
    }

    [Test]
    public void Should_Return_Failure_With_Corrupt_File_Input()
    {
        using var testHelper = new TestHelper();
        var path = testHelper.CreateCorruptFile();

        var input = DefaultInput();
        input.FilePath = path;

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = Odf.ReadSpreadsheet(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
    }

    [Test]
    public void Should_Handle_Repeated_Columns()
    {
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

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var options = DefaultOptions();
        options.ContainsHeaderRow = false;

        var result = Odf.ReadSpreadsheet(input, options, CancellationToken.None);
        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;
        var firstRow = jsonArray[0];

        Assert.That(firstRow["Column_1"]?.ToString(), Is.EqualTo("Value Test 1"));
        Assert.That(firstRow["Column_2"]?.ToString(), Is.EqualTo(string.Empty));
        Assert.That(firstRow["Column_3"]?.ToString(), Is.EqualTo(string.Empty));
        Assert.That(firstRow["Column_4"]?.ToString(), Is.EqualTo("Value Test 4"));
    }

    [Test]
    public void Should_Handle_Duplicate_Headers()
    {
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

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);
        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;
        var dataRow = jsonArray[0];

        Assert.That(dataRow["Key Test"]?.ToString(), Is.EqualTo("Value Test"));
        Assert.That(dataRow["Key Test_1"]?.ToString(), Is.EqualTo("Value Test"));
        Assert.That(dataRow["Key Test_2"]?.ToString(), Is.EqualTo("Value Test"));
    }

    [Test]
    public void Should_Handle_Unicode_Content()
    {
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

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;

        Assert.That(jsonArray[0]["AäÄaOöÖo"]?.ToString(), Is.EqualTo("ÖöÄä"));
    }

    [Test]
    public void Should_Handle_Malformed_Xml()
    {
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

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var exception = Assert.Throws<Exception>(() => Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void Should_Handle_Odf_Elements()
    {
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

        using var testHelper = new TestHelper();
        var path = testHelper.CreateTestFile(xml);

        var input = DefaultInput();
        input.FilePath = path;

        var result = Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var jsonArray = result.Data;

        Assert.That(jsonArray[0]["Key Test 1"]?.ToString(), Is.EqualTo("Value\tTest 1."));
        Assert.That(jsonArray[0]["Key Test 2"]?.ToString(), Is.EqualTo("Value     Test" + Environment.NewLine + " 2."));
    }
}