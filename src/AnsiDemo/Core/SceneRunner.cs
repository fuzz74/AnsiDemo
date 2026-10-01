using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>
/// Plays one scene: its effect layers, created in script order, on a scene-local clock over a pixel buffer of the
/// scene's mode.
/// </summary>
public sealed class SceneRunner
{
    private readonly SceneSpec _scene;
    private readonly int _seed;
    private readonly IEffect[] _layers;
    private readonly IPixelEffect[] _pixelLayers;
    private readonly ICellEffect[] _cellLayers;
    private PixelBuffer _pixels;
    private int _cols;
    private int _rows;

    public SceneRunner(SceneSpec scene, int cols, int rows, int seed = 1)
    {
        ArgumentNullException.ThrowIfNull(scene);
        _scene = scene;
        _seed = seed;
        _layers = new IEffect[scene.Effects.Count];
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i] = EffectCatalog.Create(scene.Effects[i]);
        }
        _pixelLayers = [.. _layers.OfType<IPixelEffect>()];
        _cellLayers = [.. _layers.OfType<ICellEffect>()];
        _pixels = CreatePixels(cols, rows);
        _cols = cols;
        _rows = rows;
        ResetLayers();
    }

    /// <summary>Scene time in seconds: the sum of the steps since construction.</summary>
    public double Time { get; private set; }

    /// <summary>Advances the scene time by <paramref name="dt"/> and updates every layer.</summary>
    public void Step(double dt)
    {
        if (!(dt >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(dt), dt, "A step must not go back in time.");
        }
        Time += dt;
        FrameContext ctx = Context(dt);
        foreach (IEffect layer in _layers)
        {
            layer.Update(ctx);
        }
    }

    /// <summary>Recreates the pixel buffer at the new size and resets every layer at the current time.</summary>
    public void Resize(int cols, int rows)
    {
        _pixels = CreatePixels(cols, rows);
        _cols = cols;
        _rows = rows;
        ResetLayers();
    }

    /// <summary>
    /// Clears the pixels to black, draws the pixel layers, renders the pixels into <paramref name="frame"/> and draws
    /// the cell layers on top, each kind in script order.
    /// </summary>
    public void Render(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Cols != _cols || frame.Rows != _rows)
        {
            throw new ArgumentException(
                $"The scene runs at {_cols}x{_rows} cells, the frame is {frame.Cols}x{frame.Rows}.", nameof(frame));
        }

        _pixels.Clear(Rgb.Black);
        foreach (IPixelEffect layer in _pixelLayers)
        {
            layer.Render(_pixels);
        }
        Renderers.Render(_scene.Mode, _pixels, frame);
        foreach (ICellEffect layer in _cellLayers)
        {
            layer.Render(frame);
        }
    }

    /// <summary>The scene's frame at <paramref name="seconds"/> of headless stepping from its start.</summary>
    public static Frame RenderAt(SceneSpec scene, int cols, int rows, double seconds, int seed = 1)
    {
        SceneRunner runner = SteppedTo(scene, cols, rows, seconds, seed);
        var frame = new Frame(cols, rows);
        runner.Render(frame);
        return frame;
    }

    /// <summary>A new runner stepped from its start in <see cref="DemoClock.HeadlessStep"/> steps, round(30 * seconds) of them.</summary>
    internal static SceneRunner SteppedTo(SceneSpec scene, int cols, int rows, double seconds, int seed = 1)
    {
        if (!(seconds >= 0) || !double.IsFinite(seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Scene time must be finite and non-negative.");
        }

        var runner = new SceneRunner(scene, cols, rows, seed);
        int steps = (int)Math.Round(DemoClock.HeadlessFps * seconds);
        for (int i = 0; i < steps; i++)
        {
            runner.Step(DemoClock.HeadlessStep);
        }
        return runner;
    }

    private PixelBuffer CreatePixels(int cols, int rows)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cols);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rows);
        (int width, int height) = PixelBuffer.SizeFor(_scene.Mode, cols, rows);
        return new PixelBuffer(width, height);
    }

    private void ResetLayers()
    {
        FrameContext ctx = Context(0);
        for (int i = 0; i < _layers.Length; i++)
        {
            _layers[i].Reset(ctx, new DemoRandom((uint)(_seed * 31 + i + 1)));
        }
    }

    private FrameContext Context(double deltaTime) =>
        new(Time, deltaTime, Time * DemoClock.Bpm / 60.0, _scene.Mode, _cols, _rows, _pixels.Width, _pixels.Height);
}
