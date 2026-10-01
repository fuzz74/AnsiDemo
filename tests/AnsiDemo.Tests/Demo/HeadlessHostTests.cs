using System.Buffers.Binary;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class HeadlessHostTests
{
    public const string TestEffect = "test-headless";

    private static readonly AppOptions Defaults =
        new(false, false, null, null, null, AppOptionsParser.DefaultCols, AppOptionsParser.DefaultRows, null);

    private static readonly SceneSpec Solo = SceneSpec.Solo(TestEffect, PixelMode.HalfBlock);

    private static (int Code, string Stdout, string Stderr) Run(AppOptions options)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int code = HeadlessHost.Run(options, stdout, stderr);
        return (code, stdout.ToString(), stderr.ToString());
    }

    private static void AssertUsageError(AppOptions options)
    {
        (int code, string stdout, string stderr) = Run(options);
        Assert.Equal(2, code);
        Assert.Equal("", stdout);
        Assert.Equal(AppOptionsParser.Usage + Environment.NewLine, stderr);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("4.5")]
    public void EffectFrameAtTheDefaultSizeIsTheSceneRunnersFrame(string seconds)
    {
        AppOptions options = AppOptionsParser.Parse(["--frame", seconds, "--effect", TestEffect]);

        (int code, string stdout, string stderr) = Run(options);

        Assert.Equal(0, code);
        Assert.Equal(SceneRunner.RenderAt(Solo, 80, 24, options.Frame!.Value).ToAnsi(), stdout);
        Assert.Equal("", stderr);
    }

    [Fact]
    public void EffectFrameAtACustomSizeIsTheSceneRunnersFrame()
    {
        AppOptions options = AppOptionsParser.Parse(["--frame", "2.5", "--effect", TestEffect, "--size", "123x41"]);

        (int code, string stdout, string stderr) = Run(options);

        Assert.Equal(0, code);
        Assert.Equal(SceneRunner.RenderAt(Solo, 123, 41, 2.5).ToAnsi(), stdout);
        Assert.Equal("", stderr);
    }

    [Fact]
    public void EffectFrameDependsOnTime()
    {
        string first = Run(Defaults with { Frame = 0, Effect = TestEffect }).Stdout;
        string later = Run(Defaults with { Frame = 1, Effect = TestEffect }).Stdout;

        Assert.NotEqual(first, later);
    }

    [Fact]
    public void LastStepOfTheSoloSceneIsRendered()
    {
        double last = Solo.Bars * DemoClock.SecondsPerBar - DemoClock.HeadlessStep;

        (int code, string stdout, _) = Run(Defaults with { Frame = last, Effect = TestEffect });

        Assert.Equal(0, code);
        Assert.Equal(SceneRunner.RenderAt(Solo, 80, 24, last).ToAnsi(), stdout);
    }

    [Theory]
    [InlineData(1920.0)]
    [InlineData(5000.0)]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void EffectFrameOutsideTheSoloSceneIsAUsageError(double seconds)
    {
        AssertUsageError(Defaults with { Frame = seconds, Effect = TestEffect });
    }

    [Fact]
    public void SceneFrameAtOrAfterTheScenesEndIsAUsageError()
    {
        SceneSpec boot = DemoScript.Scenes[0];
        double length = boot.Bars * DemoClock.SecondsPerBar;

        AssertUsageError(Defaults with { Frame = length, Scene = boot.Name });
        AssertUsageError(Defaults with { Frame = length + 10, Scene = boot.Name });
        AssertUsageError(Defaults with { Frame = -1, Scene = boot.Name });
    }

    [Fact]
    public void DemoFrameAtOrAfterTheEndIsAUsageError()
    {
        double total = Timeline.TotalSeconds(DemoScript.Scenes);

        AssertUsageError(AppOptionsParser.Parse(["--frame", "142.08"]));
        AssertUsageError(Defaults with { Frame = total });
        AssertUsageError(Defaults with { Frame = total + 100 });
        AssertUsageError(Defaults with { Frame = -1 });
    }

    [Fact]
    public void UnknownSceneOrEffectIsAUsageError()
    {
        AssertUsageError(Defaults with { Frame = 1, Scene = "no-such-scene" });
        AssertUsageError(Defaults with { Frame = 1, Effect = "no-such-effect" });
        AssertUsageError(Defaults with { Frame = 1, Scene = "BOOT" });
    }

    [Fact]
    public void SceneTogetherWithEffectIsAUsageError()
    {
        AssertUsageError(Defaults with { Frame = 1, Scene = "boot", Effect = TestEffect });
    }

    [Fact]
    public void NonPositiveSizeIsAUsageError()
    {
        AssertUsageError(Defaults with { Frame = 1, Effect = TestEffect, Cols = 0 });
        AssertUsageError(Defaults with { Frame = 1, Effect = TestEffect, Rows = -3 });
    }

    [Fact]
    public void NeitherFrameNorWavIsAUsageError()
    {
        AssertUsageError(Defaults);
        AssertUsageError(Defaults with { Effect = TestEffect, NoSound = true, Loop = true });
    }

    [Fact]
    public void WavWritesTheSoundtrackForTheWholeDemo()
    {
        const long Samples = 6_265_728;
        Assert.Equal(Samples, (long)Math.Round(Timeline.TotalSeconds(DemoScript.Scenes) * AudioFormat.SampleRate));
        string path = Path.Combine(Path.GetTempPath(), $"ansidemo-headless-{Guid.NewGuid():N}.wav");
        try
        {
            (int code, string stdout, string stderr) = Run(AppOptionsParser.Parse(["--wav", path]));

            Assert.Equal(0, code);
            Assert.Equal("", stdout);
            Assert.Equal("", stderr);
            byte[] bytes = File.ReadAllBytes(path);
            Assert.Equal(44 + 2 * Samples, bytes.LongLength);
            Assert.True(HasSound(bytes.AsSpan(44)), "The WAV body is silent.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static bool HasSound(ReadOnlySpan<byte> body)
    {
        for (int i = 0; i + 1 < body.Length; i += 2)
        {
            if (BinaryPrimitives.ReadInt16LittleEndian(body[i..]) != 0)
            {
                return true;
            }
        }
        return false;
    }
}
