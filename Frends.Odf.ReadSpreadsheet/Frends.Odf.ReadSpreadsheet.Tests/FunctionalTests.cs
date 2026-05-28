using System;
using System.Threading;
using Frends.Odf.ReadSpreadsheet.Definitions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Frends.Odf.ReadSpreadsheet.Tests;

[TestFixture]
internal class FunctionalTests : TestBase
{
    // Tests the main data extraction functionality using default definition parameters and the test ODF XML framework generated in TestBase.cs.
    [Test]
    public void Should_Extract_File_Data()
    {
        var result = Odf.ReadSpreadsheet(DefaultInput(), DefaultOptions(), CancellationToken.None);

        Assert.IsTrue(result.Success);

        var jsonArray = (JArray)result.Data;

        Assert.AreEqual(1, jsonArray.Count);
        Assert.AreEqual("Test 3", jsonArray[0]["Test 1"].ToString());
        Assert.AreEqual("Test 4", jsonArray[0]["Test 2"].ToString());
    }
}