using System;
using System.Threading;
using NUnit.Framework;

namespace Frends.Odf.ReadSpreadsheet.Tests;

[TestFixture]
internal class ErrorHandlerTest : TestBase
{
    private const string CustomErrorMessage = "CustomErrorMessage";

    // Fake path to trigger errors
    private readonly string fakePath = @"C:\temp\fake_path.ods";

    [Test]
    public void Should_Throw_Error_When_ThrowErrorOnFailure_Is_True()
    {
        // Inject the fake path into the default input.
        var input = DefaultInput();
        input.FilePath = fakePath;

        var ex = Assert.Throws<Exception>(() =>
           Odf.ReadSpreadsheet(input, DefaultOptions(), CancellationToken.None));

        Assert.That(ex, Is.Not.Null);
    }

    [Test]
    public void Should_Return_Failed_Result_When_ThrowErrorOnFailure_Is_False()
    {
        // Inject the fake path into the default input.
        var input = DefaultInput();
        input.FilePath = fakePath;

        var options = DefaultOptions();
        options.ThrowErrorOnFailure = false;

        var result = Odf.ReadSpreadsheet(input, options, CancellationToken.None);

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void Should_Use_Custom_ErrorMessageOnFailure()
    {
        // Inject the fake path into the default input.
        var input = DefaultInput();
        input.FilePath = fakePath;

        var options = DefaultOptions();
        options.ErrorMessageOnFailure = CustomErrorMessage;

        var ex = Assert.Throws<Exception>(() =>
             Odf.ReadSpreadsheet(input, options, CancellationToken.None));

        Assert.That(ex, Is.Not.Null);
        Assert.That(ex.Message, Contains.Substring(CustomErrorMessage));
    }
}