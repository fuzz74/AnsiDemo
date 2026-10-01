using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Core;

public class SceneRunnerTests
{
    private const int Cols = 12;
    private const int Rows = 5;
    private const int Precision = 9;

    [Fact]
    public void ConstructorCreatesTheLayersInScriptOrder()
    {
        TestLog.Clear();
        _ = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-cell", "test-rec-pixel", "test-rec-cell"), Cols, Rows);

        Assert.Collection(
            TestLog.Effects,
            effect => Assert.IsType<RecordingCellEffect>(effect),
            effect => Assert.IsType<RecordingPixelEffect>(effect),
            effect => Assert.IsType<RecordingCellEffect>(effect));
    }

    [Fact]
    public void ConstructorDoesNotLookUpTheSceneTransition()
    {
        var scene = new SceneSpec("solo", PixelMode.HalfBlock, ["test-pixel"], 4, "no-such-transition", 1);

        var runner = new SceneRunner(scene, Cols, Rows);

        Assert.Equal(0.0, runner.Time);
    }

    [Fact]
    public void ConstructorRejectsUnknownEffect()
    {
        var error = Assert.Throws<ArgumentException>(() => new SceneRunner(Scene(PixelMode.HalfBlock, "no-such-effect"), Cols, Rows));
        Assert.Contains("no-such-effect", error.Message);
    }

    [Fact]
    public void ConstructorResetsEveryLayerAtTimeZero()
    {
        TestLog.Clear();
        _ = new SceneRunner(Scene(PixelMode.Braille, "test-rec-pixel", "test-rec-cell"), Cols, Rows);

        var expected = new FrameContext(0, 0, 0, PixelMode.Braille, Cols, Rows, 2 * Cols, 4 * Rows);
        Assert.All(TestLog.Effects, effect => Assert.Equal([expected], effect.Resets));
        Assert.All(TestLog.Effects, effect => Assert.Empty(effect.Updates));
    }

    [Fact]
    public void DefaultSeedIsOne()
    {
        TestLog.Clear();
        _ = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-pixel", "test-rec-cell"), Cols, Rows);

        Assert.Equal(new DemoRandom(32).NextUInt(), TestLog.Effects[0].ResetRandoms.Single());
        Assert.Equal(new DemoRandom(33).NextUInt(), TestLog.Effects[1].ResetRandoms.Single());
    }

    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(1000)]
    public void EachLayerGetsItsOwnSeed(int seed)
    {
        TestLog.Clear();
        _ = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-pixel", "test-rec-cell", "test-rec-pixel"), Cols, Rows, seed);

        for (int layer = 0; layer < 3; layer++)
        {
            uint expected = new DemoRandom((uint)(seed * 31 + layer + 1)).NextUInt();
            Assert.Equal(expected, TestLog.Effects[layer].ResetRandoms.Single());
        }
    }

    [Fact]
    public void StepUpdatesEveryLayerWithAccumulatedTime()
    {
        TestLog.Clear();
        var runner = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-pixel", "test-rec-cell"), Cols, Rows);

        runner.Step(0.25);
        runner.Step(0.5);

        Assert.Equal(0.75, runner.Time);
        FrameContext[] expected =
        [
            new(0.25, 0.25, 0.25 * DemoClock.Bpm / 60.0, PixelMode.HalfBlock, Cols, Rows, Cols, 2 * Rows),
            new(0.75, 0.5, 0.75 * DemoClock.Bpm / 60.0, PixelMode.HalfBlock, Cols, Rows, Cols, 2 * Rows),
        ];
        Assert.All(TestLog.Effects, effect => Assert.Equal(expected, effect.Updates));
    }

    [Fact]
    public void BeatsFollowTheTempo()
    {
        TestLog.Clear();
        var runner = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-pixel"), Cols, Rows);

        runner.Step(0.75);
        runner.Step(DemoClock.SecondsPerBar - 0.75);

        RecordingEffect effect = TestLog.Effects.Single();
        Assert.Equal(1.5625, effect.Updates[0].Beats);
        Assert.Equal(DemoClock.BeatsPerBar, effect.Updates[1].Beats, Precision);
    }

    [Fact]
    public void StepRejectsNegativeTime()
    {
        var runner = new SceneRunner(Scene(PixelMode.HalfBlock, "test-pixel"), Cols, Rows);

        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Step(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => runner.Step(double.NaN));
        Assert.Equal(0.0, runner.Time);
    }

    [Fact]
    public void RenderClearsDrawsPixelLayersRendersThenDrawsCellLayers()
    {
        TestLog.Clear();
        // A cell layer first in the script still draws after the pixel layers and the renderer.
        var runner = new SceneRunner(
            Scene(PixelMode.HalfBlock, "test-rec-cell", "test-rec-pixel", "test-rec-pixel", "test-rec-cell"), Cols, Rows);
        var frame = new Frame(Cols, Rows);
        runner.Render(frame);
        frame.Write(0, 0, "junk", Rgb.White, Rgb.White);
        TestLog.Renders.Clear();

        runner.Render(frame);

        Assert.Equal(
            [
                "1 pixel saw 0,0,0",
                "2 pixel saw 101,50,25",
                "0 cell saw ▀ 102,50,25",
                "3 cell saw A 255,255,255",
            ],
            TestLog.Renders);
        Assert.Equal(new Cell('D', Rgb.White, Rgb.Black), frame[0, 0]);
        Assert.Equal(new Cell('▀', Rgb.Black, Rgb.Black), frame[1, 0]);
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock)]
    [InlineData(PixelMode.Braille)]
    public void RenderUsesTheSceneRenderer(PixelMode mode)
    {
        var runner = new SceneRunner(Scene(mode, "test-pixel"), Cols, Rows);
        var actual = new Frame(Cols, Rows);
        runner.Render(actual);

        (int width, int height) = PixelBuffer.SizeFor(mode, Cols, Rows);
        var pixels = new PixelBuffer(width, height);
        pixels.Clear(new Rgb(128, 128, 128));
        var expected = new Frame(Cols, Rows);
        Renderers.Render(mode, pixels, expected);
        Assert.Equal(expected.ToAnsi(), actual.ToAnsi());
    }

    [Fact]
    public void RenderRejectsFrameOfAnotherSize()
    {
        var runner = new SceneRunner(Scene(PixelMode.HalfBlock, "test-pixel"), Cols, Rows);

        Assert.Throws<ArgumentException>(() => runner.Render(new Frame(Cols + 1, Rows)));
        Assert.Throws<ArgumentException>(() => runner.Render(new Frame(Cols, Rows - 1)));
    }

    [Fact]
    public void ResizeResetsLayersAtTheCurrentTimeWithTheSameSeeds()
    {
        TestLog.Clear();
        var runner = new SceneRunner(Scene(PixelMode.HalfBlock, "test-rec-pixel", "test-rec-cell"), Cols, Rows, seed: 3);
        runner.Step(0.5);
        runner.Step(0.5);

        runner.Resize(20, 8);

        Assert.Equal(1.0, runner.Time);
        var expected = new FrameContext(1.0, 0, 1.0 * DemoClock.Bpm / 60.0, PixelMode.HalfBlock, 20, 8, 20, 16);
        Assert.All(TestLog.Effects, effect =>
        {
            Assert.Equal(2, effect.Resets.Count);
            Assert.Equal(expected, effect.Resets[1]);
            Assert.Equal(effect.ResetRandoms[0], effect.ResetRandoms[1]);
        });

        runner.Step(0.25);
        Assert.All(TestLog.Effects, effect => Assert.Equal(
            new FrameContext(1.25, 0.25, 1.25 * DemoClock.Bpm / 60.0, PixelMode.HalfBlock, 20, 8, 20, 16), effect.Updates[^1]));
    }

    [Fact]
    public void ResizeRecreatesTheBuffersAtTheNewSize()
    {
        var runner = new SceneRunner(Scene(PixelMode.Braille, "test-pixel", "test-cell"), Cols, Rows);

        runner.Resize(30, 9);

        var frame = new Frame(30, 9);
        runner.Render(frame);
        Assert.Equal('T', frame[0, 0].Ch);
        Assert.Equal(30 * 9, frame.LitCount());
        Assert.Throws<ArgumentException>(() => runner.Render(new Frame(Cols, Rows)));
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.51, 15)]
    [InlineData(1.0, 30)]
    [InlineData(2.49, 75)]
    public void RenderAtStepsRoundThirtyTimesSecondsTimes(double seconds, int steps)
    {
        TestLog.Clear();

        Frame frame = SceneRunner.RenderAt(Scene(PixelMode.HalfBlock, "test-rec-pixel"), Cols, Rows, seconds);

        RecordingEffect effect = TestLog.Effects.Single();
        Assert.Equal(steps, effect.Updates.Count);
        Assert.All(effect.Updates, ctx => Assert.Equal(DemoClock.HeadlessStep, ctx.DeltaTime));
        Assert.Equal(steps / 30.0, effect.Updates.LastOrDefault()?.Time ?? 0, Precision);
        Assert.Single(TestLog.Renders);
        Assert.Equal((Cols, Rows), (frame.Cols, frame.Rows));
    }

    [Fact]
    public void RenderAtPassesTheSeed()
    {
        TestLog.Clear();

        SceneRunner.RenderAt(Scene(PixelMode.HalfBlock, "test-rec-pixel"), Cols, Rows, 0.5, seed: 9);

        Assert.Equal(new DemoRandom(9 * 31 + 1).NextUInt(), TestLog.Effects.Single().ResetRandoms.Single());
    }

    [Fact]
    public void RenderAtIsDeterministic()
    {
        SceneSpec scene = Scene(PixelMode.HalfBlock, "test-noise", "test-cell");

        string first = SceneRunner.RenderAt(scene, Cols, Rows, 1.5, seed: 4).ToAnsi();
        string second = SceneRunner.RenderAt(scene, Cols, Rows, 1.5, seed: 4).ToAnsi();

        Assert.Equal(first, second);
        Assert.NotEqual(first, SceneRunner.RenderAt(scene, Cols, Rows, 1.5, seed: 5).ToAnsi());
        Assert.NotEqual(first, SceneRunner.RenderAt(scene, Cols, Rows, 1.6, seed: 4).ToAnsi());
    }

    [Fact]
    public void RenderAtRejectsNegativeOrNonFiniteTime()
    {
        SceneSpec scene = Scene(PixelMode.HalfBlock, "test-pixel");

        Assert.Throws<ArgumentOutOfRangeException>(() => SceneRunner.RenderAt(scene, Cols, Rows, -0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => SceneRunner.RenderAt(scene, Cols, Rows, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => SceneRunner.RenderAt(scene, Cols, Rows, double.PositiveInfinity));
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock)]
    [InlineData(PixelMode.Braille)]
    public void RenderDoesNotAllocateAfterTheFirstFrame(PixelMode mode)
    {
        var runner = new SceneRunner(Scene(mode, "test-noise", "test-pixel", "test-cell"), Cols, Rows);
        var frame = new Frame(Cols, Rows);
        runner.Step(DemoClock.HeadlessStep);
        runner.Render(frame);

        long before = GC.GetAllocatedBytesForCurrentThread();
        runner.Render(frame);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static SceneSpec Scene(PixelMode mode, params string[] effects) => new("test", mode, effects, 4, "test-rec", 1);
}
