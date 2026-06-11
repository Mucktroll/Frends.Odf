using System;
using System.IO;
using Frends.Odf.WriteSpreadsheet.Definitions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.Odf.WriteSpreadsheet.Tests;

internal abstract class TestBase
{
    protected string ValidTestFilePath { get; private set; }

    [SetUp]
    public void SetupBase()
    {
        ValidTestFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".ods");
    }

    [TearDown]
    public void TearDownBase()
    {
        if (File.Exists(ValidTestFilePath))
        {
            File.Delete(ValidTestFilePath);
        }
    }

    protected static Options DefaultOptions() => new();

    protected Input DefaultInput()
    {
        var jsonPayload = JArray.Parse(@"[
            { ""Name"": ""John"", ""Test"": ""Test 1"" },
            { ""Name"": ""Doe"", ""Test"": ""Test 2"" }
        ]");

        return new Input
        {
            FilePath = ValidTestFilePath,
            Payload = jsonPayload,
            ActionOnExistingFile = ActionOnExistingFile.Throw,
        };
    }
}