using System.Runtime.CompilerServices;
using System.Text;
using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Support;

public static class Golden
{
    private const string UpdateVariable = "ANSIDEMO_UPDATE_GOLDENS";

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// Compares frame.ToAnsi() with "<directory of callerFile>/<name>.ansi", ignoring '\r'. Missing file: writes it and
    /// fails with "golden recorded, rerun". Mismatch: writes "<name>.actual.ansi" beside it and fails with both paths.
    /// Environment variable ANSIDEMO_UPDATE_GOLDENS=1 overwrites and passes.
    public static void Assert(Frame frame, string name, [CallerFilePath] string callerFile = "")
    {
        string directory = Path.GetDirectoryName(callerFile)
            ?? throw new ArgumentException($"No directory in caller file path '{callerFile}'.", nameof(callerFile));
        string goldenPath = Path.Combine(directory, name + ".ansi");
        string actual = frame.ToAnsi();

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(goldenPath, actual, Utf8NoBom);
            return;
        }

        if (!File.Exists(goldenPath))
        {
            File.WriteAllText(goldenPath, actual, Utf8NoBom);
            Xunit.Assert.Fail($"{goldenPath}: golden recorded, rerun");
        }

        string expected = File.ReadAllText(goldenPath, Utf8NoBom);
        if (expected.Replace("\r", "") != actual.Replace("\r", ""))
        {
            string actualPath = Path.Combine(directory, name + ".actual.ansi");
            File.WriteAllText(actualPath, actual, Utf8NoBom);
            Xunit.Assert.Fail($"Frame differs from golden {goldenPath}; actual frame written to {actualPath}");
        }
    }
}
