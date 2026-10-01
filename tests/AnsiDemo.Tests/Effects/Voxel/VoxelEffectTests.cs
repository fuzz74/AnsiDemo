using System.Runtime.CompilerServices;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Effects.Voxel;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Effects.Voxel;

public class VoxelEffectTests
{
    private const string Name = "voxel";
    private static readonly Rgb Marker = new(255, 0, 255);

    [Fact]
    public void SameCallTwiceGivesIdenticalAnsi()
    {
        string first = SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24, 2.5).ToAnsi();
        string second = SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24, 2.5).ToAnsi();

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(0, "t0")]
    [InlineData(1, "t1")]
    [InlineData(4, "t4")]
    public void FrameMatchesGolden(double seconds, string golden)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24, seconds);

        Golden.Assert(frame, golden);
    }

    [Fact]
    public void FrameAtOneSecondIsAnimatedAndLit()
    {
        Frame t0 = SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24, 0);
        Frame t1 = SceneRunner.RenderAt(SceneSpec.Solo(Name, PixelMode.HalfBlock), 80, 24, 1);

        Assert.NotEqual(t0.ToAnsi(), t1.ToAnsi());
        Assert.True(t1.LitCount() >= 39, $"Only {t1.LitCount()} of 1920 cells are lit.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void RendersInBothModesAtMinimumAndMaximumSize(PixelMode mode, int cols, int rows)
    {
        Frame frame = SceneRunner.RenderAt(SceneSpec.Solo(Name, mode), cols, rows, 1);

        Assert.Equal(cols, frame.Cols);
        Assert.True(frame.LitCount() > 0);
    }

    [Fact]
    [Trait("Category", "Perf")]
    public void MeetsFrameBudget()
    {
        double ms = Perf.MeanFrameMs(Name, PixelMode.HalfBlock, 200, 60);

        Assert.True(ms <= 33, $"Mean frame time {ms:F1} ms exceeds 33 ms.");
    }

    [Theory]
    [InlineData(PixelMode.HalfBlock, 80, 24)]
    [InlineData(PixelMode.HalfBlock, 200, 60)]
    [InlineData(PixelMode.Braille, 80, 24)]
    [InlineData(PixelMode.Braille, 200, 60)]
    public void LeavesSkyUntouchedAndDrawsGround(PixelMode mode, int cols, int rows)
    {
        (int width, int height) = PixelBuffer.SizeFor(mode, cols, rows);
        var effect = new VoxelEffect();
        var pixels = new PixelBuffer(width, height);
        effect.Reset(new FrameContext(0, 0, 0, mode, cols, rows, width, height), new DemoRandom(1));

        for (int step = 0; step <= 300; step++)
        {
            double time = step * DemoClock.HeadlessStep;
            if (step > 0)
            {
                effect.Update(new FrameContext(time, DemoClock.HeadlessStep, time * DemoClock.Bpm / 60.0, mode, cols, rows, width, height));
            }
            if (step % 30 != 0)
            {
                continue;
            }

            pixels.Clear(Marker);
            effect.Render(pixels);

            for (int y = 0; y < height / 10; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Assert.True(pixels[x, y] == Marker, $"Sky pixel ({x}, {y}) overwritten at t = {time:F1} s.");
                }
            }
            for (int y = height * 9 / 10; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Assert.True(pixels[x, y] != Marker, $"Ground pixel ({x}, {y}) not drawn at t = {time:F1} s.");
                }
            }
        }
    }

    [Fact]
    public void SourceAvoidsBannedApis()
    {
        string folder = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Effects", "Voxel");
        string[] files = Directory.GetFiles(folder, "*.cs");
        Assert.NotEmpty(files);

        var hits = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                foreach (string banned in new[] { "System.Random", "DateTime", "Stopwatch", "Console." })
                {
                    if (lines[i].Contains(banned, StringComparison.Ordinal))
                    {
                        hits.Add($"{Path.GetFileName(file)}:{i + 1}: {banned}");
                    }
                }
            }
        }
        Assert.Empty(hits);
    }

    private static string FindRepoRoot([CallerFilePath] string callerFile = "")
    {
        for (string? dir = Path.GetDirectoryName(callerFile); dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (File.Exists(Path.Combine(dir, "AnsiDemo.slnx")))
            {
                return dir;
            }
        }
        throw new InvalidOperationException($"AnsiDemo.slnx not found above {callerFile}.");
    }
}
