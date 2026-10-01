using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class AppOptionsParserTests
{
    public const string TestEffect = "test-options";

    private static readonly AppOptions Defaults = new(false, false, null, null, null, 80, 24, null);

    [Fact]
    public void NoArgumentsGiveTheDefaults()
    {
        Assert.Equal(Defaults, AppOptionsParser.Parse([]));
    }

    [Fact]
    public void PlayFormAcceptsItsFlagsInAnyOrder()
    {
        Assert.Equal(Defaults with { NoSound = true }, AppOptionsParser.Parse(["--no-sound"]));
        Assert.Equal(Defaults with { Loop = true }, AppOptionsParser.Parse(["--loop"]));
        Assert.Equal(Defaults with { Scene = "fire" }, AppOptionsParser.Parse(["--scene", "fire"]));
        Assert.Equal(Defaults with { Effect = TestEffect }, AppOptionsParser.Parse(["--effect", TestEffect]));
        Assert.Equal(
            Defaults with { NoSound = true, Loop = true, Scene = "spheres" },
            AppOptionsParser.Parse(["--scene", "spheres", "--loop", "--no-sound"]));
        Assert.Equal(
            Defaults with { NoSound = true, Loop = true, Effect = TestEffect },
            AppOptionsParser.Parse(["--no-sound", "--effect", TestEffect, "--loop"]));
    }

    [Fact]
    public void FrameFormAcceptsSceneEffectAndSize()
    {
        Assert.Equal(Defaults with { Frame = 20 }, AppOptionsParser.Parse(["--frame", "20"]));
        Assert.Equal(Defaults with { Frame = 0 }, AppOptionsParser.Parse(["--frame", "0"]));
        Assert.Equal(
            Defaults with { Frame = 1.5, Scene = "title", Cols = 120, Rows = 40 },
            AppOptionsParser.Parse(["--frame", "1.5", "--scene", "title", "--size", "120x40"]));
        Assert.Equal(
            Defaults with { Frame = 30, Effect = TestEffect, Cols = 200, Rows = 60 },
            AppOptionsParser.Parse(["--size", "200x60", "--effect", TestEffect, "--frame", "3e1"]));
        Assert.Equal(Defaults with { Frame = 0.25, Cols = 1, Rows = 1 }, AppOptionsParser.Parse(["--frame", ".25", "--size", "1x1"]));
    }

    [Fact]
    public void FrameHasNoUpperBoundInTheParser()
    {
        Assert.Equal(Defaults with { Frame = 100000 }, AppOptionsParser.Parse(["--frame", "100000"]));
    }

    [Fact]
    public void WavFormTakesOnlyAPath()
    {
        Assert.Equal(Defaults with { Wav = "out.wav" }, AppOptionsParser.Parse(["--wav", "out.wav"]));
        Assert.Equal(Defaults with { Wav = "C:\\tmp\\demo song.wav" }, AppOptionsParser.Parse(["--wav", "C:\\tmp\\demo song.wav"]));
    }

    [Fact]
    public void EveryScriptSceneIsAccepted()
    {
        Assert.All(DemoScript.Scenes, scene => Assert.Equal(scene.Name, AppOptionsParser.Parse(["--scene", scene.Name]).Scene));
    }

    // Arguments are separated by single spaces.
    [Theory]
    // Unknown arguments.
    [InlineData("--bogus")]
    [InlineData("fire")]
    [InlineData("-h")]
    [InlineData("--help")]
    [InlineData("--Loop")]
    [InlineData("--frame=1")]
    [InlineData("--loop extra")]
    // Repeated flags.
    [InlineData("--no-sound --no-sound")]
    [InlineData("--loop --loop")]
    [InlineData("--scene fire --scene title")]
    [InlineData("--scene fire --scene fire")]
    [InlineData("--effect test-options --effect test-options")]
    [InlineData("--frame 1 --frame 2")]
    [InlineData("--frame 1 --size 80x24 --size 80x24")]
    [InlineData("--wav a.wav --wav b.wav")]
    // Missing values.
    [InlineData("--scene")]
    [InlineData("--effect")]
    [InlineData("--frame")]
    [InlineData("--frame 1 --size")]
    [InlineData("--wav")]
    [InlineData("--scene --loop")]
    [InlineData("--frame --scene fire")]
    [InlineData("--wav --loop")]
    // --scene together with --effect.
    [InlineData("--scene fire --effect test-options")]
    [InlineData("--effect test-options --scene fire")]
    [InlineData("--frame 1 --scene fire --effect test-options")]
    // --size without --frame.
    [InlineData("--size 80x24")]
    [InlineData("--scene fire --size 80x24")]
    [InlineData("--loop --size 80x24")]
    // --no-sound or --loop with --frame.
    [InlineData("--frame 1 --no-sound")]
    [InlineData("--loop --frame 1")]
    [InlineData("--frame 1 --scene fire --loop")]
    // --wav combined with anything.
    [InlineData("--wav a.wav --loop")]
    [InlineData("--no-sound --wav a.wav")]
    [InlineData("--wav a.wav --frame 1")]
    [InlineData("--wav a.wav --scene fire")]
    [InlineData("--wav a.wav --effect test-options")]
    [InlineData("--frame 1 --size 80x24 --wav a.wav")]
    // --frame not a finite number >= 0.
    [InlineData("--frame -1")]
    [InlineData("--frame -0.001")]
    [InlineData("--frame abc")]
    [InlineData("--frame NaN")]
    [InlineData("--frame Infinity")]
    [InlineData("--frame -Infinity")]
    [InlineData("--frame 1e400")]
    [InlineData("--frame 1,5")]
    [InlineData("--frame 0x10")]
    [InlineData("--frame 1s")]
    // --size not two positive integers separated by a lowercase x.
    [InlineData("--frame 1 --size 80X24")]
    [InlineData("--frame 1 --size 80")]
    [InlineData("--frame 1 --size 80x")]
    [InlineData("--frame 1 --size x24")]
    [InlineData("--frame 1 --size 0x24")]
    [InlineData("--frame 1 --size 80x0")]
    [InlineData("--frame 1 --size -80x24")]
    [InlineData("--frame 1 --size +80x24")]
    [InlineData("--frame 1 --size 80x24x1")]
    [InlineData("--frame 1 --size 80*24")]
    [InlineData("--frame 1 --size 80.5x24")]
    [InlineData("--frame 1 --size eightyx24")]
    [InlineData("--frame 1 --size 99999999999x24")]
    // Scene not in the script (ordinal, case-sensitive).
    [InlineData("--scene nope")]
    [InlineData("--scene Fire")]
    [InlineData("--scene plasma")]
    [InlineData("--frame 1 --scene nope")]
    // Effect not in the catalog (ordinal, case-sensitive).
    [InlineData("--effect nope")]
    [InlineData("--effect Test-Options")]
    [InlineData("--effect boot")]
    [InlineData("--effect test-cut")]
    [InlineData("--frame 1 --effect nope")]
    public void InvalidArgumentsThrowTheUsage(string args)
    {
        AssertUsageError(args.Split(' '));
    }

    [Theory]
    [InlineData("--scene")]
    [InlineData("--effect")]
    [InlineData("--frame")]
    [InlineData("--wav")]
    public void EmptyValueIsMissing(string flag)
    {
        AssertUsageError([flag, ""]);
    }

    [Fact]
    public void EmptySizeIsMissing()
    {
        AssertUsageError(["--frame", "1", "--size", ""]);
    }

    [Fact]
    public void EmptyArgumentIsUnknown()
    {
        AssertUsageError([""]);
    }

    [Fact]
    public void UsageIsTheFourLinesOfTheSpec()
    {
        Assert.Equal(
            [
                "Usage: AnsiDemo [--no-sound] [--loop] [--scene <name> | --effect <name>]",
                "       AnsiDemo --frame <seconds> [--scene <name> | --effect <name>] [--size <cols>x<rows>]",
                "       AnsiDemo --wav <path>",
                "Keys: Q/Esc quit, Space pause, Left/Right previous/next scene",
            ],
            AppOptionsParser.Usage.Split('\n'));
        Assert.DoesNotContain('\r', AppOptionsParser.Usage);
    }

    private static void AssertUsageError(string[] args)
    {
        var error = Assert.Throws<ArgumentException>(() => AppOptionsParser.Parse(args));
        Assert.Equal(AppOptionsParser.Usage, error.Message);
    }
}
