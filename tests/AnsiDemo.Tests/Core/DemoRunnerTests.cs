using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Core;

public class DemoRunnerTests
{
    private const int Cols = 12;
    private const int Rows = 5;
    private const double Bar = DemoClock.SecondsPerBar;
    private const double Step = DemoClock.HeadlessStep;
    private const int Precision = 9;

    // Scenes of recording effects, to watch which runners exist and how they are stepped.
    private static readonly IReadOnlyList<SceneSpec> Recorded =
    [
        new("first", PixelMode.HalfBlock, ["test-rec-pixel"], 2, "test-rec", 1),
        new("second", PixelMode.HalfBlock, ["test-rec-cell"], 2, "test-rec", 1),
    ];

    // Scenes whose frames depend only on time and seed, to compare frames from different runners.
    private static readonly IReadOnlyList<SceneSpec> Plain =
    [
        new("noise", PixelMode.HalfBlock, ["test-noise"], 2, "test-rec", 1),
        new("grey", PixelMode.Braille, ["test-pixel", "test-cell"], 1, "test-rec", 1),
        new("text", PixelMode.HalfBlock, ["test-noise", "test-cell"], 2, "test-rec", 1),
    ];

    [Fact]
    public void ConstructorRejectsAnEmptyDemo()
    {
        Assert.Throws<ArgumentException>(() => new DemoRunner([], Cols, Rows));
    }

    [Fact]
    public void BeforeTheFirstStepTheCueAtZeroApplies()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        var frame = new Frame(Cols, Rows);

        runner.Render(frame);

        Assert.Equal(0.0, runner.Time);
        Assert.False(runner.Finished);
        Assert.IsType<RecordingPixelEffect>(Assert.Single(TestLog.Effects));
        TransitionCall call = Assert.Single(TestLog.Transitions);
        Assert.Equal(new Frame(Cols, Rows).ToAnsi(), call.From);
        Assert.Equal(0.0, call.Progress);
        Assert.Equal(RecordingTransition.Marker, frame[0, 0]);
    }

    [Fact]
    public void SceneZeroFadesInFromBlank()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        StepUntil(runner, 0.5 * Bar);

        runner.Render(new Frame(Cols, Rows));

        TransitionCall call = Assert.Single(TestLog.Transitions);
        Assert.Equal(new Frame(Cols, Rows).ToAnsi(), call.From);
        Assert.Equal(PixelFrame(Color(0)), call.To);
        Assert.Equal(Timeline.CueAt(Recorded, runner.Time)!.Progress, call.Progress);
    }

    [Fact]
    public void FromSceneIsKeptDuringTheTransitionAndDroppedAfterIt()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        var frame = new Frame(Cols, Rows);
        double start = Timeline.StartOf(Recorded, 1);
        StepUntil(runner, start);

        Assert.Equal(2, TestLog.Effects.Count);
        RecordingEffect first = TestLog.Effects[0];
        RecordingEffect second = Assert.IsType<RecordingCellEffect>(TestLog.Effects[1]);
        Assert.Equal(0.0, second.Resets.Single().Time);
        Assert.Equal(runner.Time - start, second.Updates.Single().Time, Precision);
        // The from-scene keeps stepping past its own bars.
        Assert.Equal(runner.Time, first.Updates[^1].Time, Precision);
        Assert.True(first.Updates[^1].Time > 2 * Bar);
        Assert.Equal(Step, first.Updates[^1].DeltaTime);

        runner.Render(frame);
        runner.Step(Step);
        runner.Render(frame);

        Assert.Equal(2, TestLog.Transitions.Count);
        Assert.Same(TestLog.Transitions[0].Transition, TestLog.Transitions[1].Transition);
        TransitionCall call = TestLog.Transitions[1];
        Assert.Equal(PixelFrame(Color(0)), call.From);
        Assert.Equal(CellFrame('B'), call.To);
        Assert.Equal(Timeline.CueAt(Recorded, runner.Time)!.Progress, call.Progress);
        Assert.Equal(RecordingTransition.Marker, frame[0, 0]);

        StepUntil(runner, start + Bar);
        int updates = first.Updates.Count;
        Assert.True(first.Updates[^1].Time < start + Bar);
        runner.Step(Step);
        runner.Render(frame);

        Assert.Equal(updates, first.Updates.Count);
        Assert.Equal(2, TestLog.Transitions.Count);
        Assert.Equal(CellFrame('B'), frame.ToAnsi());
        Assert.Equal(2, TestLog.Effects.Count);
    }

    [Fact]
    public void FromSceneIsSteppedByTheSameStepsAsTheScene()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        StepUntil(runner, Timeline.StartOf(Recorded, 1));

        runner.Step(0.05);

        Assert.Equal(0.05, TestLog.Effects[0].Updates[^1].DeltaTime);
        Assert.Equal(0.05, TestLog.Effects[1].Updates[^1].DeltaTime);
    }

    [Fact]
    public void SeekCreatesOnlyTheCuesSceneSteppedFromItsStart()
    {
        var runner = new DemoRunner(Recorded, Cols, Rows);
        double start = Timeline.StartOf(Recorded, 1);
        double seconds = start + 0.5 * Bar;
        TestLog.Clear();

        runner.Seek(seconds);
        var frame = new Frame(Cols, Rows);
        runner.Render(frame);

        Assert.Equal(seconds, runner.Time);
        RecordingEffect effect = Assert.IsType<RecordingCellEffect>(Assert.Single(TestLog.Effects));
        Assert.Equal((int)Math.Round(30 * (seconds - start)), effect.Updates.Count);
        Assert.All(effect.Updates, ctx => Assert.Equal(Step, ctx.DeltaTime));
        Assert.Empty(TestLog.Transitions);
        Assert.Equal(CellFrame(((RecordingCellEffect)effect).Ch), frame.ToAnsi());
    }

    [Fact]
    public void SeekRendersTheSceneAloneEvenInsideItsTransition()
    {
        var runner = new DemoRunner(Plain, Cols, Rows);
        double start = Timeline.StartOf(Plain, 1);
        TestLog.Clear();

        runner.Seek(start + 0.25 * Bar);
        var frame = new Frame(Cols, Rows);
        runner.Render(frame);

        Assert.Empty(TestLog.Transitions);
        Assert.Equal(SceneRunner.RenderAt(Plain[1], Cols, Rows, 0.25 * Bar).ToAnsi(), frame.ToAnsi());
    }

    [Fact]
    public void SeekBackwardsReplacesTheRunners()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        StepUntil(runner, Timeline.StartOf(Recorded, 1) + 0.5 * Bar);
        TestLog.Clear();

        runner.Seek(0.5);
        runner.Render(new Frame(Cols, Rows));

        Assert.Equal(0.5, runner.Time);
        Assert.IsType<RecordingPixelEffect>(Assert.Single(TestLog.Effects));
        Assert.Equal(15, TestLog.Effects[0].Updates.Count);
        Assert.Empty(TestLog.Transitions);
    }

    [Fact]
    public void SeekRejectsNegativeTime()
    {
        var runner = new DemoRunner(Recorded, Cols, Rows);

        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Seek(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Seek(double.NaN));
    }

    [Fact]
    public void StepRejectsNegativeTime()
    {
        var runner = new DemoRunner(Recorded, Cols, Rows);

        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Step(-Step));
        Assert.Equal(0.0, runner.Time);
    }

    [Fact]
    public void FinishedWhenTimeReachesTheEnd()
    {
        var runner = new DemoRunner(Recorded, Cols, Rows);
        var frame = new Frame(Cols, Rows);
        double total = Timeline.TotalSeconds(Recorded);

        while (!runner.Finished)
        {
            Assert.True(runner.Time < total);
            runner.Step(Step);
        }

        Assert.True(runner.Time >= total);
        runner.Render(frame);
        runner.Step(Step);
        runner.Render(frame);
        Assert.True(runner.Finished);
    }

    [Fact]
    public void SeekToTheEndFinishes()
    {
        var runner = new DemoRunner(Recorded, Cols, Rows);
        var frame = new Frame(Cols, Rows);

        runner.Seek(Timeline.TotalSeconds(Recorded));
        runner.Render(frame);
        runner.Step(Step);
        runner.Render(frame);

        Assert.True(runner.Finished);
    }

    [Fact]
    public void ResizeResetsTheActiveScenesAndKeepsTime()
    {
        TestLog.Clear();
        var runner = new DemoRunner(Recorded, Cols, Rows);
        StepUntil(runner, Timeline.StartOf(Recorded, 1) + 0.5 * Bar);
        double time = runner.Time;
        RecordingEffect first = TestLog.Effects[0], second = TestLog.Effects[1];

        runner.Resize(20, 8);

        Assert.Equal(time, runner.Time);
        Assert.Equal(new FrameContext(first.Updates[^1].Time, 0, first.Updates[^1].Beats, PixelMode.HalfBlock, 20, 8, 20, 16), first.Resets[^1]);
        Assert.Equal(new FrameContext(second.Updates[^1].Time, 0, second.Updates[^1].Beats, PixelMode.HalfBlock, 20, 8, 20, 16), second.Resets[^1]);
        Assert.Equal(first.ResetRandoms[0], first.ResetRandoms[^1]);

        TestLog.Transitions.Clear();
        var frame = new Frame(20, 8);
        runner.Render(frame);
        Assert.Equal(RecordingTransition.Marker, frame[0, 0]);
        TransitionCall call = Assert.Single(TestLog.Transitions);
        Assert.Equal(8, call.From.Count(c => c == '\n'));
        Assert.Equal(8, call.To.Count(c => c == '\n'));
        Assert.Throws<ArgumentException>(() => runner.Render(new Frame(Cols, Rows)));
    }

    [Fact]
    public void RenderRejectsFrameOfAnotherSize()
    {
        var runner = new DemoRunner(Plain, Cols, Rows);

        Assert.Throws<ArgumentException>(() => runner.Render(new Frame(Cols - 1, Rows)));
    }

    [Fact]
    public void RenderDoesNotAllocateInTheSteadyState()
    {
        IReadOnlyList<SceneSpec> scenes =
        [
            new("a", PixelMode.Braille, ["test-pixel"], 1, "test-cut", 1),
            new("b", PixelMode.HalfBlock, ["test-noise", "test-cell"], 2, "test-cut", 1),
        ];
        var runner = new DemoRunner(scenes, Cols, Rows);
        var frame = new Frame(Cols, Rows);
        StepUntil(runner, Timeline.StartOf(scenes, 1) + 0.5 * Bar);
        runner.Render(frame);

        long before = GC.GetAllocatedBytesForCurrentThread();
        runner.Render(frame);
        long inTransition = GC.GetAllocatedBytesForCurrentThread() - before;
        StepUntil(runner, Timeline.StartOf(scenes, 1) + 1.5 * Bar);
        runner.Render(frame);
        before = GC.GetAllocatedBytesForCurrentThread();
        runner.Render(frame);
        long alone = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, inTransition);
        Assert.Equal(0, alone);
    }

    [Fact]
    public void RenderAtCombinesTheFromSceneAndTheScene()
    {
        double seconds = Timeline.StartOf(Plain, 2) + 0.25 * Bar;
        SceneCue cue = Timeline.CueAt(Plain, seconds)!;
        TestLog.Clear();

        Frame frame = DemoRunner.RenderAt(Plain, Cols, Rows, seconds);

        TransitionCall call = Assert.Single(TestLog.Transitions);
        Assert.Equal(0.25, call.Progress, Precision);
        // The from-scene's time is measured from its own start.
        Assert.Equal(SceneRunner.RenderAt(Plain[1], Cols, Rows, cue.FromSceneTime).ToAnsi(), call.From);
        Assert.NotEqual(SceneRunner.RenderAt(Plain[1], Cols, Rows, seconds).ToAnsi(), call.From);
        Frame to = SceneRunner.RenderAt(Plain[2], Cols, Rows, cue.SceneTime);
        Assert.Equal(to.ToAnsi(), call.To);
        to[0, 0] = RecordingTransition.Marker;
        Assert.Equal(to.ToAnsi(), frame.ToAnsi());
    }

    [Fact]
    public void RenderAtFadesSceneZeroFromBlank()
    {
        TestLog.Clear();

        DemoRunner.RenderAt(Plain, Cols, Rows, 0.5 * Bar);

        TransitionCall call = Assert.Single(TestLog.Transitions);
        Assert.Equal(new Frame(Cols, Rows).ToAnsi(), call.From);
        Assert.Equal(SceneRunner.RenderAt(Plain[0], Cols, Rows, 0.5 * Bar).ToAnsi(), call.To);
        Assert.Equal(0.5, call.Progress, Precision);
    }

    [Fact]
    public void RenderAtWithoutFromSceneIsTheSceneAlone()
    {
        double start = Timeline.StartOf(Plain, 2);
        TestLog.Clear();

        Frame frame = DemoRunner.RenderAt(Plain, Cols, Rows, start + 1.5 * Bar);

        Assert.Empty(TestLog.Transitions);
        Assert.Equal(SceneRunner.RenderAt(Plain[2], Cols, Rows, 1.5 * Bar).ToAnsi(), frame.ToAnsi());
    }

    [Fact]
    public void RenderAtIsDeterministic()
    {
        double seconds = Timeline.StartOf(Plain, 1) + 0.4 * Bar;

        Assert.Equal(DemoRunner.RenderAt(Plain, Cols, Rows, seconds).ToAnsi(), DemoRunner.RenderAt(Plain, Cols, Rows, seconds).ToAnsi());
    }

    [Fact]
    public void RenderAtRejectsTimeOutsideTheDemo()
    {
        double total = Timeline.TotalSeconds(Plain);

        Assert.Throws<ArgumentOutOfRangeException>(() => DemoRunner.RenderAt(Plain, Cols, Rows, -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => DemoRunner.RenderAt(Plain, Cols, Rows, total));
        Assert.Throws<ArgumentOutOfRangeException>(() => DemoRunner.RenderAt(Plain, Cols, Rows, total + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => DemoRunner.RenderAt(Plain, Cols, Rows, double.NaN));
    }

    private static void StepUntil(DemoRunner runner, double seconds)
    {
        while (runner.Time < seconds)
        {
            runner.Step(Step);
        }
    }

    private static Rgb Color(int id) => new((byte)(100 + id), 50, 25);

    /// <summary>A half-block frame of black pixels with <paramref name="topLeft"/> at pixel (0, 0).</summary>
    private static string PixelFrame(Rgb topLeft)
    {
        var pixels = new PixelBuffer(Cols, 2 * Rows);
        pixels[0, 0] = topLeft;
        var frame = new Frame(Cols, Rows);
        HalfBlockRenderer.Render(pixels, frame);
        return frame.ToAnsi();
    }

    /// <summary>A half-block frame of black pixels with <paramref name="ch"/> written at (0, 0).</summary>
    private static string CellFrame(char ch)
    {
        var frame = new Frame(Cols, Rows);
        HalfBlockRenderer.Render(new PixelBuffer(Cols, 2 * Rows), frame);
        frame[0, 0] = new Cell(ch, Rgb.White, Rgb.Black);
        return frame.ToAnsi();
    }
}
