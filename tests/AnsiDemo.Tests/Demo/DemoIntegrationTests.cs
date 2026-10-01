using System.Diagnostics;
using System.Globalization;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class DemoIntegrationTests
{
    private static readonly TimeSpan FrameBudget = TimeSpan.FromSeconds(10);

    public static TheoryData<string> SceneNames => new(DemoScript.Scenes.Select(scene => scene.Name));

    private static readonly string[] ScriptEffects =
        DemoScript.Scenes.SelectMany(scene => scene.Effects).Distinct(StringComparer.Ordinal).ToArray();

    public static TheoryData<string> EffectNames => new(ScriptEffects);

    public static TheoryData<int> SceneIndexes => DemoGoldenTests.SceneIndexes;

    private static (int Code, string Stdout, string Stderr) Run(params string[] args)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        int code = HeadlessHost.Run(AppOptionsParser.Parse(args), stdout, stderr);
        return (code, stdout.ToString(), stderr.ToString());
    }

    private static string Seconds(double t) => t.ToString("R", CultureInfo.InvariantCulture);

    [Fact]
    public void ScriptUsesTheTwelveEffects()
    {
        Assert.Equal(12, ScriptEffects.Length);
    }

    [Theory]
    [MemberData(nameof(EffectNames))]
    public void EveryEffectOfTheScriptResolvesThroughTheCatalog(string name)
    {
        Assert.Contains(name, EffectCatalog.Names);
        IEffect effect = EffectCatalog.Create(name);
        Assert.True(effect is IPixelEffect or ICellEffect, $"{name} is neither a pixel nor a cell effect.");
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void EveryTransitionOfTheScriptResolvesThroughTheCatalog(string scene)
    {
        string name = DemoScript.Scenes.Single(s => s.Name == scene).Transition;
        Assert.Contains(name, TransitionCatalog.Names);
        Assert.NotNull(TransitionCatalog.Create(name));
    }

    [Fact]
    public void DemoFrameIsTheDemoRunnersFrame()
    {
        (int code, string stdout, string stderr) = Run("--frame", "20");

        Assert.Equal(0, code);
        Assert.Equal(DemoRunner.RenderAt(DemoScript.Scenes, 80, 24, 20).ToAnsi(), stdout);
        Assert.Equal("", stderr);
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void SceneFrameIsTheSceneRunnersFrame(string name)
    {
        SceneSpec scene = DemoScript.Scenes.Single(s => s.Name == name);

        (int code, string stdout, string stderr) = Run("--frame", "1.5", "--scene", name);

        Assert.Equal(0, code);
        Assert.Equal(SceneRunner.RenderAt(scene, 80, 24, 1.5).ToAnsi(), stdout);
        Assert.Equal("", stderr);
    }

    [Theory]
    [MemberData(nameof(EffectNames))]
    public void EffectFrameIsTheSoloScenesFrameInThePreferredMode(string name)
    {
        SceneSpec solo = SceneSpec.Solo(name, EffectCatalog.PreferredMode(name));

        (int code, string stdout, string stderr) = Run("--frame", "1", "--effect", name);

        Assert.Equal(0, code);
        Assert.Equal(SceneRunner.RenderAt(solo, 80, 24, 1).ToAnsi(), stdout);
        Assert.Equal("", stderr);
    }

    [Fact]
    public void DemoFrameAtTheEndIsAUsageError()
    {
        (int code, string stdout, string stderr) = Run("--frame", Seconds(Timeline.TotalSeconds(DemoScript.Scenes)));

        Assert.Equal(2, code);
        Assert.Equal("", stdout);
        Assert.Equal(AppOptionsParser.Usage + Environment.NewLine, stderr);
    }

    [Theory]
    [MemberData(nameof(SceneIndexes))]
    public void FrameInTheMiddleOfATransitionFinishesInTime(int i)
    {
        AssertFrameFinishesInTime(DemoGoldenTests.MidTransition(i));
    }

    [Fact]
    public void LastFrameOfTheDemoFinishesInTime()
    {
        AssertFrameFinishesInTime(Timeline.TotalSeconds(DemoScript.Scenes) - DemoClock.HeadlessStep);
    }

    private static void AssertFrameFinishesInTime(double t)
    {
        var clock = Stopwatch.StartNew();
        (int code, string stdout, _) = Run("--frame", Seconds(t));
        clock.Stop();

        Assert.Equal(0, code);
        Assert.NotEqual("", stdout);
        Assert.True(clock.Elapsed <= FrameBudget, $"--frame {Seconds(t)} took {clock.Elapsed.TotalSeconds:F2} s.");
    }
}
