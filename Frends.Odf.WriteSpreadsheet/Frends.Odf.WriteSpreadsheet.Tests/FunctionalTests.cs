using System;
using System.IO;
using System.Threading;
using Frends.Odf.WriteSpreadsheet.Definitions;
using Frends.Odf.WriteSpreadsheet.Tests.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.Odf.WriteSpreadsheet.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    [Test]
    public void Should_Write_Input_Data()
    {
        var input = DefaultInput();
        var options = DefaultOptions();

        var result = Odf.WriteSpreadsheet(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.True, "Task failed to execute successfully.");
        Assert.That(File.Exists(result.FilePath), Is.True, "The output .ods file was not created.");

        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("John"));
        Assert.That(xmlString, Contains.Substring("Test 1"));
        Assert.That(xmlString, Contains.Substring("Doe"));
        Assert.That(xmlString, Contains.Substring("Test 2"));
    }

    [Test]
    public void Should_Generate_Headers_If_IncludeHeaderRow_Is_True()
    {
        var input = DefaultInput();
        var options = DefaultOptions();
        options.IncludeHeaderRow = true;

        var result = Odf.WriteSpreadsheet(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("Name"));
        Assert.That(xmlString, Contains.Substring("Test"));
    }

    [Test]
    public void Should_Throw_When_Input_FilePath_Is_Incorrect()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "fake_path.ods");

        var input = DefaultInput();
        input.FilePath = path;

        var exception = Assert.Throws<Exception>(() => Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(exception.Message, Contains.Substring("Destination directory not found"));
    }

    [Test]
    public void Should_Throw_When_Input_Data_Is_Incorrect()
    {
        var invalidPayload = JObject.Parse(@"{ ""Name"": ""John"" }");

        var input = DefaultInput();
        input.Payload = invalidPayload;

        var exception = Assert.Throws<Exception>(() => Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(exception.Message, Contains.Substring("must be a valid array of objects"));
    }

    [Test]
    public void Should_Throw_When_ActionOnExistingFile_Is_Throw()
    {
        File.WriteAllText(ValidTestFilePath, "This is an existing file.");

        var input = DefaultInput();
        input.ActionOnExistingFile = ActionOnExistingFile.Throw;

        var exception = Assert.Throws<Exception>(() => Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(exception.Message, Contains.Substring("File already exists"));
    }

    [Test]
    public void Should_Overwrite_When_ActionOnExistingFile_Is_Overwrite()
    {
        File.WriteAllText(ValidTestFilePath, "This is an existing file.");

        var input = DefaultInput();
        input.ActionOnExistingFile = ActionOnExistingFile.Overwrite;

        var result = Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(File.Exists(result.FilePath), Is.True);

        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("John"));
        Assert.That(xmlString, Does.Not.Contain("This is an existing file."));
    }

    [Test]
    public void Should_Write_Empty_Spreadsheet_With_Empty_Payload()
    {
        var emptyPayload = JArray.Parse("[]");

        var input = DefaultInput();
        input.Payload = emptyPayload;

        var options = DefaultOptions();
        options.IncludeHeaderRow = false;

        var result = Odf.WriteSpreadsheet(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(File.Exists(result.FilePath), Is.True);

        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Does.Not.Contain("John"));
        Assert.That(xmlString, Does.Not.Contain("table:table-cell>"));
    }

    [Test]
    public void Should_Handle_Unicode_Content()
    {
        var unicodePayload = JArray.Parse(@"[
            { ""Text1"": ""AäÄaOöÖo."" },
            { ""Text2"": ""ÖöÄä."" }
        ]");

        var input = DefaultInput();
        input.Payload = unicodePayload;

        var result = Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("AäÄaOöÖo."));
        Assert.That(xmlString, Contains.Substring("ÖöÄä."));
    }

    [Test]
    public void Should_Escape_Formula_Injection()
    {
        var formulaPayload = JArray.Parse(@"[
            { ""Equals"": ""=SUM(A1:A2)"", ""Plus"": ""+100"", ""Minus"": ""-50"", ""At"": ""@Test"" }
        ]");

        var input = DefaultInput();
        input.Payload = formulaPayload;

        var result = Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("'=SUM(A1:A2)"));
        Assert.That(xmlString, Contains.Substring("'+100"));
        Assert.That(xmlString, Contains.Substring("'-50"));
        Assert.That(xmlString, Contains.Substring("'@Test"));
    }

    [Test]
    public void Should_Handle_Partially_Empty_Json()
    {
        var partialPayload = JArray.Parse(@"[
            { ""Col1"": ""Row1"" },
            { ""Col2"": ""Row2"" }
        ]");

        var input = DefaultInput();
        input.Payload = partialPayload;

        var result = Odf.WriteSpreadsheet(input, DefaultOptions(), CancellationToken.None);

        Assert.That(result.Success, Is.True);
        var xmlString = TestHelper.ReadOdsContent(result.FilePath);

        Assert.That(xmlString, Contains.Substring("Col1"));
        Assert.That(xmlString, Contains.Substring("Col2"));
        Assert.That(xmlString, Contains.Substring("Row1"));
        Assert.That(xmlString, Contains.Substring("Row2"));
    }
}