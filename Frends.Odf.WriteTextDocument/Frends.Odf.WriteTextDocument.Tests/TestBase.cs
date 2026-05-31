using System;
using dotenv.net;
using Frends.Odf.WriteTextDocument.Definitions;

namespace Frends.Odf.WriteTextDocument.Tests;

internal abstract class TestBase
{
    internal TestBase()
    {
        // TODO: Here you can load environment variables used in tests
        DotEnv.Load();
        SecretKey = GetEnvVar("FRENDS_SECRET_KEY");
    }

    // TODO: Replace with your secret key or remove if not needed
    protected string SecretKey { get; set; }

    protected static Input DefaultInput() => new();

    protected static Connection DefaultConnection() => new();

    protected static Options DefaultOptions() => new();

    private static string GetEnvVar(string name) => Environment.GetEnvironmentVariable(name) ??
                                                    throw new InvalidOperationException(
                                                        $"Missing required env var: {name}");
}
