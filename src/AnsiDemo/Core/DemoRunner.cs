using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>
/// Plays a list of scenes along the <see cref="Timeline"/>: the cue's scene, and during its transition the scene
/// before it (or a blank frame before the first scene), combined by the scene's transition.
/// </summary>
public sealed class DemoRunner
{
    private const int Seed = 1;

    private readonly SceneSpec[] _scenes;
    private readonly double _totalSeconds;
    private readonly ITransition?[] _transitions;
    private int _cols;
    private int _rows;
    private Frame _fromFrame;
    private Frame _toFrame;

    private int _sceneIndex;
    private SceneRunner _scene;
    private bool _inTransition;
    private SceneRunner? _fromScene;   // null during a transition from the blank frame
    private double _progress;

    public DemoRunner(IReadOnlyList<SceneSpec> scenes, int cols, int rows)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        _scenes = [.. scenes];
        SceneCue cue = Timeline.CueAt(_scenes, 0)
            ?? throw new ArgumentException("The demo needs at least one scene with a positive length.", nameof(scenes));
        _totalSeconds = Timeline.TotalSeconds(_scenes);
        _transitions = new ITransition?[_scenes.Length];
        (_fromFrame, _toFrame) = (new Frame(cols, rows), new Frame(cols, rows));
        _cols = cols;
        _rows = rows;

        _sceneIndex = cue.Scene;
        _scene = new SceneRunner(_scenes[cue.Scene], cols, rows, Seed);
        _inTransition = cue.FromScene == -1;
        _progress = cue.Progress;
        if (_inTransition)
        {
            Transition(cue.Scene);
        }
    }

    /// <summary>Demo time in seconds.</summary>
    public double Time { get; private set; }

    public bool Finished => Time >= _totalSeconds;

    /// <summary>
    /// Advances the demo time by <paramref name="dt"/>. Entering a new scene creates its runner and keeps the previous
    /// scene's runner, still stepping, until the transition ends.
    /// </summary>
    public void Step(double dt)
    {
        if (!(dt >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(dt), dt, "A step must not go back in time.");
        }
        Time += dt;

        if (Timeline.CueAt(_scenes, Time) is not { } cue)
        {
            // Past the end: the last scene plays on alone.
            EndTransition();
            _scene.Step(dt);
            return;
        }

        if (cue.Scene != _sceneIndex)
        {
            SceneRunner previous = _scene;
            bool fromPrevious = cue.FromScene == _sceneIndex;
            _sceneIndex = cue.Scene;
            _scene = new SceneRunner(_scenes[cue.Scene], _cols, _rows, Seed);
            _inTransition = fromPrevious;
            _fromScene = fromPrevious ? previous : null;
            if (_inTransition)
            {
                Transition(cue.Scene);
            }
            _fromScene?.Step(dt);
            // The new scene starts at scene time 0, part of the way into this step.
            _scene.Step(cue.SceneTime);
        }
        else
        {
            if (cue.FromScene is null)
            {
                EndTransition();
            }
            _fromScene?.Step(dt);
            _scene.Step(dt);
        }
        _progress = cue.Progress;
    }

    /// <summary>
    /// Jumps to <paramref name="seconds"/>: only the cue's scene is created, stepped from its start in headless steps,
    /// and it renders alone, without a transition.
    /// </summary>
    public void Seek(double seconds)
    {
        if (!(seconds >= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Demo time must not be negative.");
        }
        Time = seconds;
        EndTransition();

        // At or past the end the demo is finished and keeps showing the current scene.
        if (Timeline.CueAt(_scenes, seconds) is { } cue)
        {
            _sceneIndex = cue.Scene;
            _scene = SceneRunner.SteppedTo(_scenes[cue.Scene], _cols, _rows, cue.SceneTime, Seed);
            _progress = cue.Progress;
        }
    }

    /// <summary>Recreates the buffers at the new size and resets the active scenes; the time is kept.</summary>
    public void Resize(int cols, int rows)
    {
        (_fromFrame, _toFrame) = (new Frame(cols, rows), new Frame(cols, rows));
        _cols = cols;
        _rows = rows;
        _scene.Resize(cols, rows);
        _fromScene?.Resize(cols, rows);
    }

    /// <summary>The current scene, combined with the from-scene (or a blank frame) by its transition while one runs.</summary>
    public void Render(Frame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Cols != _cols || frame.Rows != _rows)
        {
            throw new ArgumentException(
                $"The demo runs at {_cols}x{_rows} cells, the frame is {frame.Cols}x{frame.Rows}.", nameof(frame));
        }

        if (!_inTransition)
        {
            _scene.Render(frame);
            return;
        }

        _scene.Render(_toFrame);
        if (_fromScene is null)
        {
            _fromFrame.Clear();
        }
        else
        {
            _fromScene.Render(_fromFrame);
        }
        Transition(_sceneIndex).Apply(_fromFrame, _toFrame, _progress, frame);
    }

    /// <summary>
    /// The demo's frame at <paramref name="seconds"/>, each scene stepped headlessly from its own start;
    /// <see cref="ArgumentOutOfRangeException"/> outside 0 &lt;= seconds &lt; TotalSeconds.
    /// </summary>
    public static Frame RenderAt(IReadOnlyList<SceneSpec> scenes, int cols, int rows, double seconds)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        SceneCue cue = Timeline.CueAt(scenes, seconds)
            ?? throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "The time is outside the demo.");

        SceneSpec scene = scenes[cue.Scene];
        Frame to = SceneRunner.RenderAt(scene, cols, rows, cue.SceneTime, Seed);
        if (cue.FromScene is not { } fromScene)
        {
            return to;
        }

        Frame from = fromScene < 0
            ? new Frame(cols, rows)
            : SceneRunner.RenderAt(scenes[fromScene], cols, rows, cue.FromSceneTime, Seed);
        var output = new Frame(cols, rows);
        TransitionCatalog.Create(scene.Transition).Apply(from, to, cue.Progress, output);
        return output;
    }

    private ITransition Transition(int scene) => _transitions[scene] ??= TransitionCatalog.Create(_scenes[scene].Transition);

    private void EndTransition()
    {
        _inTransition = false;
        _fromScene = null;
    }
}
