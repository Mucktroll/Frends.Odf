using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using NUnit.Framework;

namespace Frends.Odf.ReadTextDocument.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    // Tests the main read functionality using default definition parameters and the test ODF XML framework generated in TestBase.cs.
    [Test]
    public void Should_Extract_File_Content()
    {
        var result = Odf.ReadTextDocument(DefaultInput(), DefaultOptions(), CancellationToken.None);

        Assert.IsTrue(result.Success, "Task failed to execute successfully.");

        // Compare the extracted content from the Task with the expected output.
        var expectedOutput = "Test Heading" + Environment.NewLine + "Test paragraph 1." + Environment.NewLine + "Test paragraph 2.";
        Assert.AreEqual(expectedOutput, result.Content);
    }

    [Test]
    public void Should_Throw_When_ContentXml_Is_Missing()
    {
        // Creates a temporary .odt zip file without injecting content.xml.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".odt");
        ZipFile.Open(path, ZipArchiveMode.Create).Dispose();

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Tries to extract content.xml content.
            var exception = Assert.Throws<Exception>(() => Odf.ReadTextDocument(input, DefaultOptions(), CancellationToken.None));

            // Confirms exception message matches the expected message when content.xml is missing.
            Assert.That(exception.Message, Contains.Substring("content.xml is missing"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Return_Empty_String()
    {
        // Empty XML framework with no paragraph or header content.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:text>
                    </office:text>
                </office:body>
            </office:document-content>";

        // Path of the created .odt file injected with the empty XML content.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts content.xml content.
            var result = Odf.ReadTextDocument(input, DefaultOptions(), CancellationToken.None);

            // Checks that the Task returned successfully and that the extracted content is an empty string.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(string.Empty, result.Content);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Should_Return_Failure_With_Corrupt_File_Input()
    {
        // Creates a temporary .odt file with standard text instead of a valid Zip archive.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".odt");
        File.WriteAllText(path, "This is corrupt file input.");

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            var options = DefaultOptions();
            options.ThrowErrorOnFailure = false;

            // Extracts the corrupt file input.
            var result = Odf.ReadTextDocument(input, options, CancellationToken.None);

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
    public void Should_Handle_Unicode_Content()
    {
        // XML framework with Scandinavian characters.
        var xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:text>
                        <text:h>AäÄaOöÖo.</text:h>
                        <text:p>ÖöÄä.</text:p>
                    </office:text>
                </office:body>
            </office:document-content>";

        // Path of the created .odt file injected with the Scandinavian XML content.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts content.xml content.
            var result = Odf.ReadTextDocument(input, DefaultOptions(), CancellationToken.None);

            var expectedOutput = "AäÄaOöÖo." + Environment.NewLine + "ÖöÄä.";

            // Checks that the Task returned successfully and that the extracted content matches the known input.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(expectedOutput, result.Content);
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
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:text>
                        <text:p>This tag never closes.
                    </office:text>
                </office:body>
            </office:document-content>";

        // Path of the created .odt file injected with the malformed XML.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts the malformed XML input.
            var exception = Assert.Throws<Exception>(() => Odf.ReadTextDocument(input, DefaultOptions(), CancellationToken.None));

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
            <office:document-content xmlns:office=""urn:oasis:names:tc:opendocument:xmlns:office:1.0"" xmlns:text=""urn:oasis:names:tc:opendocument:xmlns:text:1.0"">
                <office:body>
                    <office:text>
                        <text:p>Test<text:tab/>Paragraph 1.</text:p>
                        <text:p>Test<text:s text:c=""4""/> Paragraph<text:line-break/> 2.</text:p>
                    </office:text>
                </office:body>
            </office:document-content>";

        // Path of the created .odt file injected with the ODF elements.
        var path = Helpers.TestHelper.CreateTestFile(xml);

        try
        {
            var input = DefaultInput();
            input.FilePath = path;

            // Extracts the ODF elements input.
            var result = Odf.ReadTextDocument(input, DefaultOptions(), CancellationToken.None);

            var expectedOutput = "Test\tParagraph 1." + Environment.NewLine + "Test     Paragraph" + Environment.NewLine + " 2.";

            // Checks that the Task returned successfully and that the extracted content matches the known input.
            Assert.IsTrue(result.Success);
            Assert.AreEqual(expectedOutput, result.Content);
        }
        finally
        {
            File.Delete(path);
        }
    }
}