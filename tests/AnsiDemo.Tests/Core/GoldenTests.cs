using System.Text;
using AnsiDemo.Contracts;
using AnsiDemo.Tests.Support;
using Xunit.Sdk;

namespace AnsiDemo.Tests.Core;

public sealed class GoldenTests : IDisposable
{
    private const string UpdateVariable = "ANSIDEMO_UPDATE_GOLDENS";

    private readonly string? _savedUpdate = Environment.GetEnvironmentVariable(UpdateVariable);
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "AnsiDemo.Tests", "Golden-" + Guid.NewGuid().ToString("N"));
    private readonly string _callerFile;

    public GoldenTests()
    {
        Environment.SetEnvironmentVariable(UpdateVariable, null);
        Directory.CreateDirectory(_directory);
        _callerFile = Path.Combine(_directory, "SomeEffectTests.cs");
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(UpdateVariable, _savedUpdate);
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void MissingGoldenIsRecordedAndFails()
    {
        Frame frame = MakeFrame('▀');

        var failure = Assert.Throws<FailException>(() => Golden.Assert(frame, "t0", _callerFile));

        Assert.Contains("golden recorded, rerun", failure.Message);
        string golden = Path.Combine(_directory, "t0.ansi");
        Assert.Equal(frame.ToAnsi(), File.ReadAllText(golden, Encoding.UTF8));
        Assert.False(StartsWithBom(golden));
    }

    [Fact]
    public void MatchingGoldenPasses()
    {
        Frame frame = MakeFrame('▀');
        Assert.Throws<FailException>(() => Golden.Assert(frame, "t1", _callerFile));

        Golden.Assert(frame, "t1", _callerFile);

        Assert.False(File.Exists(Path.Combine(_directory, "t1.actual.ansi")));
    }

    [Fact]
    public void CarriageReturnsInTheGoldenAreIgnored()
    {
        Frame frame = MakeFrame('#');
        File.WriteAllText(Path.Combine(_directory, "crlf.ansi"), frame.ToAnsi().Replace("\n", "\r\n"));

        Golden.Assert(frame, "crlf", _callerFile);
    }

    [Fact]
    public void MismatchWritesActualBesideTheGoldenAndFailsWithBothPaths()
    {
        Frame recorded = MakeFrame('▀');
        Frame changed = MakeFrame('⣿');
        Assert.Throws<FailException>(() => Golden.Assert(recorded, "t4", _callerFile));

        var failure = Assert.Throws<FailException>(() => Golden.Assert(changed, "t4", _callerFile));

        string golden = Path.Combine(_directory, "t4.ansi");
        string actual = Path.Combine(_directory, "t4.actual.ansi");
        Assert.Contains(golden, failure.Message);
        Assert.Contains(actual, failure.Message);
        Assert.Equal(recorded.ToAnsi(), File.ReadAllText(golden, Encoding.UTF8));
        Assert.Equal(changed.ToAnsi(), File.ReadAllText(actual, Encoding.UTF8));
        Assert.False(StartsWithBom(actual));
    }

    [Fact]
    public void UpdateVariableOverwritesAndPasses()
    {
        Frame recorded = MakeFrame('▀');
        Frame changed = MakeFrame('⣿');
        string golden = Path.Combine(_directory, "update.ansi");

        Environment.SetEnvironmentVariable(UpdateVariable, "1");
        Golden.Assert(recorded, "update", _callerFile);
        Assert.Equal(recorded.ToAnsi(), File.ReadAllText(golden, Encoding.UTF8));

        Golden.Assert(changed, "update", _callerFile);
        Assert.Equal(changed.ToAnsi(), File.ReadAllText(golden, Encoding.UTF8));
        Assert.False(StartsWithBom(golden));

        Environment.SetEnvironmentVariable(UpdateVariable, null);
        Golden.Assert(changed, "update", _callerFile);
        Assert.Throws<FailException>(() => Golden.Assert(recorded, "update", _callerFile));
    }

    private static Frame MakeFrame(char ch)
    {
        var frame = new Frame(4, 2);
        frame.Write(0, 0, new string(ch, 3), new Rgb(255, 128, 0), new Rgb(0, 0, 64));
        frame[3, 1] = new Cell(ch, Rgb.White, new Rgb(10, 20, 30));
        return frame;
    }

    private static bool StartsWithBom(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        return bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    }
}
