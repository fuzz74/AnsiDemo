using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Core;

public class TimelineTests
{
    private const double Bar = DemoClock.SecondsPerBar;
    private const int Precision = 9;

    // Bars and transition bars of the demo's scene table (4.3).
    private static readonly IReadOnlyList<SceneSpec> Demo =
    [
        Scene(8, 1), Scene(10, 1), Scene(10, 1), Scene(8, 1),
        Scene(8, 1), Scene(10, 1), Scene(10, 1), Scene(10, 1),
    ];

    [Fact]
    public void StartOfSumsTheLengthsOfThePreviousScenes()
    {
        IReadOnlyList<SceneSpec> scenes = [Scene(8, 1), Scene(10, 2), Scene(3, 0)];
        Assert.Equal(0.0, Timeline.StartOf(scenes, 0));
        Assert.Equal(8 * Bar, Timeline.StartOf(scenes, 1), Precision);
        Assert.Equal(18 * Bar, Timeline.StartOf(scenes, 2), Precision);
        Assert.Equal(21 * Bar, Timeline.StartOf(scenes, 3), Precision);
    }

    [Fact]
    public void TotalSecondsSumsAllScenes()
    {
        Assert.Equal(74 * Bar, Timeline.TotalSeconds(Demo), Precision);
        Assert.Equal(DemoClock.DemoBars * Bar, Timeline.TotalSeconds(Demo), Precision);
        Assert.Equal(0.0, Timeline.TotalSeconds([]));
    }

    [Fact]
    public void StartOfRejectsIndexOutsideTheScenes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.StartOf(Demo, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Timeline.StartOf(Demo, Demo.Count + 1));
    }

    [Fact]
    public void CueAtStartIsSceneZeroFadingFromBlank()
    {
        Assert.Equal(new SceneCue(0, 0, -1, 0, 0), Timeline.CueAt(Demo, 0));
    }

    [Fact]
    public void SceneZeroTransitionEndsAfterItsBars()
    {
        AssertCue(new SceneCue(0, 0.5 * Bar, -1, 0, 0.5), Timeline.CueAt(Demo, 0.5 * Bar));
        AssertCue(new SceneCue(0, Math.BitDecrement(Bar), -1, 0, 1), Timeline.CueAt(Demo, Math.BitDecrement(Bar)));
        Assert.Equal(new SceneCue(0, Bar, null, 0, 1), Timeline.CueAt(Demo, Bar));
    }

    [Fact]
    public void SceneBoundaryBelongsToTheNextScene()
    {
        double start = Timeline.StartOf(Demo, 1);
        AssertCue(new SceneCue(1, 0, 0, start, 0), Timeline.CueAt(Demo, start));

        SceneCue? before = Timeline.CueAt(Demo, Math.BitDecrement(start));
        Assert.NotNull(before);
        Assert.Equal(0, before.Scene);
        Assert.Null(before.FromScene);
    }

    [Fact]
    public void OutsideTheDemoIsNull()
    {
        double total = Timeline.TotalSeconds(Demo);
        Assert.Null(Timeline.CueAt(Demo, -0.001));
        Assert.Null(Timeline.CueAt(Demo, Math.BitDecrement(0.0)));
        Assert.Null(Timeline.CueAt(Demo, total));
        Assert.Null(Timeline.CueAt(Demo, total + 1));
        Assert.Null(Timeline.CueAt(Demo, double.NaN));
        Assert.Null(Timeline.CueAt([], 0));
    }

    [Fact]
    public void LastInstantBelongsToTheLastScene()
    {
        SceneCue? cue = Timeline.CueAt(Demo, Math.BitDecrement(Timeline.TotalSeconds(Demo)));
        Assert.NotNull(cue);
        Assert.Equal(7, cue.Scene);
        Assert.Null(cue.FromScene);
        Assert.Equal(1.0, cue.Progress);
    }

    [Fact]
    public void ZeroTransitionBarsMeansNoTransition()
    {
        IReadOnlyList<SceneSpec> scenes = [Scene(2, 0), Scene(2, 0)];
        Assert.Equal(new SceneCue(0, 0, null, 0, 1), Timeline.CueAt(scenes, 0));
        AssertCue(new SceneCue(1, 0, null, 0, 1), Timeline.CueAt(scenes, 2 * Bar));

        SceneCue? solo = Timeline.CueAt([SceneSpec.Solo("plasma", PixelMode.HalfBlock)], 0);
        Assert.Equal(new SceneCue(0, 0, null, 0, 1), solo);
    }

    [Fact]
    public void LongerTransitionScalesProgress()
    {
        IReadOnlyList<SceneSpec> scenes = [Scene(4, 0), Scene(4, 2)];
        AssertCue(new SceneCue(1, Bar, 0, 5 * Bar, 0.5), Timeline.CueAt(scenes, 5 * Bar));
        AssertCue(new SceneCue(1, 1.5 * Bar, 0, 5.5 * Bar, 0.75), Timeline.CueAt(scenes, 5.5 * Bar));
        AssertCue(new SceneCue(1, 2 * Bar, null, 0, 1), Timeline.CueAt(scenes, 6 * Bar));
    }

    [Theory]
    // t, scene, scene time, from scene (-2 = null), from-scene time, progress
    [InlineData(0.48, 0, 0.48, -1, 0, 0.25)]
    [InlineData(5.0, 0, 5.0, -2, 0, 1)]
    [InlineData(15.36, 1, 0, 0, 15.36, 0)]
    [InlineData(16.32, 1, 0.96, 0, 16.32, 0.5)]
    [InlineData(17.28, 1, 1.92, -2, 0, 1)]
    [InlineData(36.0, 2, 1.44, 1, 20.64, 0.75)]
    [InlineData(54.24, 3, 0.48, 2, 19.68, 0.25)]
    [InlineData(100.0, 5, 15.52, -2, 0, 1)]
    [InlineData(123.84, 7, 0.96, 6, 20.16, 0.5)]
    [InlineData(142.0, 7, 19.12, -2, 0, 1)]
    public void DemoTableCues(double t, int scene, double sceneTime, int from, double fromTime, double progress)
    {
        AssertCue(new SceneCue(scene, sceneTime, from == -2 ? null : from, fromTime, progress), Timeline.CueAt(Demo, t));
    }

    private static SceneSpec Scene(int bars, int transitionBars) =>
        new($"scene{bars}", PixelMode.HalfBlock, ["test-pixel"], bars, "test-cut", transitionBars);

    private static void AssertCue(SceneCue expected, SceneCue? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Scene, actual.Scene);
        Assert.Equal(expected.SceneTime, actual.SceneTime, Precision);
        Assert.Equal(expected.FromScene, actual.FromScene);
        Assert.Equal(expected.FromSceneTime, actual.FromSceneTime, Precision);
        Assert.Equal(expected.Progress, actual.Progress, Precision);
    }
}
