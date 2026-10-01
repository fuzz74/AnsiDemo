using AnsiDemo.Audio;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Music;
using AnsiDemo.Shell;
using AnsiDemo.Tracker;

namespace AnsiDemo.Tests.Demo;

public class ConsolePlaybackTests
{
    private const double Bar = DemoClock.SecondsPerBar;

    // Three scenes of 2 bars each: 0..3.84, 3.84..7.68, 7.68..11.52 s.
    private static readonly IReadOnlyList<SceneSpec> Scenes =
    [
        new("one", PixelMode.HalfBlock, [ConsoleTestEffect.Name], 2, "crossfade", 1),
        new("two", PixelMode.HalfBlock, [ConsoleTestEffect.Name], 2, "wipe", 1),
        new("three", PixelMode.Braille, [ConsoleTestEffect.Name], 2, "push", 0),
    ];

    [Theory]
    [InlineData(0.0, 1, 2 * Bar)]
    [InlineData(0.0, -1, 0.0)]
    [InlineData(1.0, -1, 0.0)]
    [InlineData(2 * Bar, 1, 4 * Bar)]
    [InlineData(2 * Bar + 0.5, -1, 0.0)]
    [InlineData(5 * Bar, -1, 2 * Bar)]
    [InlineData(5 * Bar, 1, 4 * Bar)]
    [InlineData(1.0, 99, 4 * Bar)]
    [InlineData(5 * Bar, -99, 0.0)]
    [InlineData(6 * Bar, -1, 2 * Bar)]
    [InlineData(100.0, 1, 4 * Bar)]
    public void SceneStartFromClampsToTheFirstAndLastScene(double seconds, int offset, double expected)
    {
        Assert.Equal(expected, ConsolePlayback.SceneStartFrom(Scenes, seconds, offset), 9);
    }

    [Fact]
    public void SceneStartFromWalksTheDemoScript()
    {
        IReadOnlyList<SceneSpec> scenes = DemoScript.Scenes;
        for (int i = 0; i < scenes.Count; i++)
        {
            double middle = Timeline.StartOf(scenes, i) + 0.5;
            Assert.Equal(Timeline.StartOf(scenes, Math.Min(i + 1, scenes.Count - 1)), ConsolePlayback.SceneStartFrom(scenes, middle, 1));
            Assert.Equal(Timeline.StartOf(scenes, Math.Max(i - 1, 0)), ConsolePlayback.SceneStartFrom(scenes, middle, -1));
        }
    }

    [Fact]
    public void DemoStepsTimeAndRendersItsScene()
    {
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song: null, loop: false);
        playback.Step(1.5);
        playback.Step(0.75);   // past the 1-bar fade in from the blank frame

        Assert.Equal(2.25, playback.Time, 9);
        Assert.False(playback.Finished);
        Assert.Equal((80, 24), (playback.Cols, playback.Rows));
        var frame = new Frame(80, 24);
        playback.Render(frame);
        Assert.StartsWith("CONSOLE", frame.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void PauseStopsTimeAndTheSongUntilResumed()
    {
        SongPlayer song = Song();
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song, loop: false);
        playback.Step(1);

        playback.TogglePause();
        playback.Step(1);
        Assert.True(playback.Paused);
        Assert.True(song.Paused);
        Assert.Equal(1, playback.Time, 9);

        playback.TogglePause();
        playback.Step(1);
        Assert.False(playback.Paused);
        Assert.False(song.Paused);
        Assert.Equal(2, playback.Time, 9);
    }

    [Fact]
    public void SkippingSeeksTheDemoAndTheSongToTheSceneStart()
    {
        SongPlayer song = Song();
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song, loop: false);
        playback.Step(1);

        playback.SkipScenes(1);
        song.Render(Span<short>.Empty);   // the seek is applied at the next render
        Assert.Equal(2 * Bar, playback.Time, 9);
        Assert.Equal(2 * Bar, song.PositionSeconds, 6);

        playback.SkipScenes(1);
        playback.SkipScenes(1);
        Assert.Equal(4 * Bar, playback.Time, 9);

        playback.SkipScenes(-1);
        song.Render(Span<short>.Empty);
        Assert.Equal(2 * Bar, playback.Time, 9);
        Assert.Equal(2 * Bar, song.PositionSeconds, 6);
    }

    [Fact]
    public void SkippingWorksWhilePaused()
    {
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song: null, loop: false);
        playback.TogglePause();
        playback.SkipScenes(1);

        Assert.Equal(2 * Bar, playback.Time, 9);
        Assert.True(playback.Paused);
    }

    [Fact]
    public void DemoFinishesAtItsEndWithoutLoop()
    {
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song: null, loop: false);
        playback.SkipScenes(2);
        playback.Step(2 * Bar - 0.01);
        Assert.False(playback.Finished);

        playback.Step(0.02);
        Assert.True(playback.Finished);
    }

    [Fact]
    public void LoopRestartsTheDemoAndTheSongAtZero()
    {
        SongPlayer song = Song();
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song, loop: true);
        playback.SkipScenes(2);
        song.Render(Span<short>.Empty);
        playback.Step(2 * Bar + 0.01);

        song.Render(Span<short>.Empty);
        Assert.False(playback.Finished);
        Assert.Equal(0, playback.Time);
        Assert.Equal(0, song.PositionSeconds);
    }

    [Fact]
    public void ResizeKeepsTimeAndRendersAtTheNewSize()
    {
        var playback = ConsolePlayback.ForDemo(Scenes, 80, 24, song: null, loop: false);
        playback.Step(1);

        playback.Resize(120, 40);

        Assert.Equal((120, 40), (playback.Cols, playback.Rows));
        Assert.Equal(1, playback.Time, 9);
        var frame = new Frame(120, 40);
        playback.Render(frame);
        Assert.StartsWith("CONSOLE", frame.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void SingleSceneRestartsAfterItsBarsAndNeverFinishes()
    {
        var scene = new SceneSpec("solo", PixelMode.Braille, [ConsoleTestEffect.Name], 1, "wipe", 1);
        var playback = ConsolePlayback.ForScene(scene, 80, 24);

        playback.Step(1);
        Assert.Equal(1, playback.Time, 9);
        playback.Step(1);
        Assert.Equal(0, playback.Time);
        Assert.False(playback.Finished);

        playback.Step(0.5);
        Assert.Equal(0.5, playback.Time, 9);
        var frame = new Frame(80, 24);
        playback.Render(frame);
        Assert.StartsWith("CONSOLE", frame.ToText(), StringComparison.Ordinal);
    }

    [Fact]
    public void SingleSceneIgnoresSkipsButPauses()
    {
        var playback = ConsolePlayback.ForScene(SceneSpec.Solo(ConsoleTestEffect.Name, PixelMode.HalfBlock), 80, 24);
        playback.Step(1);

        playback.SkipScenes(1);
        playback.SkipScenes(-1);
        Assert.Equal(1, playback.Time, 9);

        playback.TogglePause();
        playback.Step(1);
        Assert.Equal(1, playback.Time, 9);
    }

    [Fact]
    public void SingleSceneResizeKeepsItsTime()
    {
        var playback = ConsolePlayback.ForScene(SceneSpec.Solo(ConsoleTestEffect.Name, PixelMode.HalfBlock), 80, 24);
        playback.Step(3);

        playback.Resize(100, 30);

        Assert.Equal(3, playback.Time, 9);
        playback.Render(new Frame(100, 30));
    }

    private static SongPlayer Song() => new(SongParser.Parse(Soundtrack.Text), new Synth(), loop: false);
}
