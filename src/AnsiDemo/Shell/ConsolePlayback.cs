using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Tracker;

namespace AnsiDemo.Shell;

/// <summary>
/// What the <see cref="ConsoleHost"/> plays, without the terminal: either the whole demo on a <see cref="DemoRunner"/>
/// with an optional song kept at the same time, or a single scene on a <see cref="SceneRunner"/> that restarts each
/// time its bars have elapsed. Pausing stops both time and song.
/// </summary>
public sealed class ConsolePlayback
{
    private readonly IReadOnlyList<SceneSpec> _scenes;
    private readonly DemoRunner? _demo;
    private readonly SongPlayer? _song;
    private readonly bool _loop;
    private readonly SceneSpec? _soloScene;
    private readonly double _soloSeconds;
    private SceneRunner? _solo;

    private ConsolePlayback(IReadOnlyList<SceneSpec> scenes, DemoRunner? demo, SongPlayer? song, bool loop,
                            SceneSpec? soloScene, int cols, int rows)
    {
        _scenes = scenes;
        _demo = demo;
        _song = song;
        _loop = loop;
        _soloScene = soloScene;
        if (soloScene is not null)
        {
            _soloSeconds = soloScene.Bars * DemoClock.SecondsPerBar;
            _solo = new SceneRunner(soloScene, cols, rows);
        }
        Cols = cols;
        Rows = rows;
    }

    /// <summary>
    /// The whole demo from t = 0. <paramref name="song"/> (null for silence) is paused and sought along with the
    /// demo; with <paramref name="loop"/> both restart at 0 when the demo ends.
    /// </summary>
    public static ConsolePlayback ForDemo(IReadOnlyList<SceneSpec> scenes, int cols, int rows, SongPlayer? song, bool loop)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        return new ConsolePlayback(scenes, new DemoRunner(scenes, cols, rows), song, loop, null, cols, rows);
    }

    /// <summary>Only <paramref name="scene"/>, without transitions or music, restarted after its bars, forever.</summary>
    public static ConsolePlayback ForScene(SceneSpec scene, int cols, int rows)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return new ConsolePlayback([scene], null, null, loop: true, scene, cols, rows);
    }

    public int Cols { get; private set; }
    public int Rows { get; private set; }
    public bool Paused { get; private set; }

    /// <summary>Demo time, or the scene time of the single scene.</summary>
    public double Time => _demo?.Time ?? _solo!.Time;

    /// <summary>True once the demo has reached its end without <c>loop</c>; a single scene never finishes.</summary>
    public bool Finished => _demo is { Finished: true };

    /// <summary>Advances time by <paramref name="dt"/> unless paused, restarting where the playback loops.</summary>
    public void Step(double dt)
    {
        if (Paused)
        {
            return;
        }
        if (_demo is not null)
        {
            _demo.Step(dt);
            if (_demo.Finished && _loop)
            {
                _demo.Seek(0);
                _song?.Seek(0);
            }
            return;
        }

        _solo!.Step(dt);
        if (_solo.Time >= _soloSeconds)
        {
            _solo = new SceneRunner(_soloScene!, Cols, Rows);
        }
    }

    public void Render(Frame frame)
    {
        if (_demo is not null)
        {
            _demo.Render(frame);
        }
        else
        {
            _solo!.Render(frame);
        }
    }

    /// <summary>Recreates the buffers at the new size and resets the active scenes; time is kept.</summary>
    public void Resize(int cols, int rows)
    {
        if (_demo is not null)
        {
            _demo.Resize(cols, rows);
        }
        else
        {
            _solo!.Resize(cols, rows);
        }
        Cols = cols;
        Rows = rows;
    }

    public void TogglePause()
    {
        Paused = !Paused;
        if (_song is not null)
        {
            _song.Paused = Paused;
        }
    }

    /// <summary>
    /// Jumps the demo and the song to the start of the scene <paramref name="offset"/> scenes from the current one
    /// (+1 next, −1 previous), clamped to the first and last scene. A single scene ignores this.
    /// </summary>
    public void SkipScenes(int offset)
    {
        if (_demo is null)
        {
            return;
        }
        double target = SceneStartFrom(_scenes, _demo.Time, offset);
        _demo.Seek(target);
        _song?.Seek(target);
    }

    /// <summary>
    /// Start of the scene <paramref name="offset"/> scenes from the one playing at <paramref name="seconds"/>, clamped
    /// to the first and last scene; at or past the end the last scene counts as playing.
    /// </summary>
    public static double SceneStartFrom(IReadOnlyList<SceneSpec> scenes, double seconds, int offset)
    {
        ArgumentNullException.ThrowIfNull(scenes);
        ArgumentOutOfRangeException.ThrowIfZero(scenes.Count);
        int current = Timeline.CueAt(scenes, seconds)?.Scene
                      ?? (seconds >= Timeline.TotalSeconds(scenes) ? scenes.Count - 1 : 0);
        int target = (int)Math.Clamp((long)current + offset, 0, scenes.Count - 1);
        return Timeline.StartOf(scenes, target);
    }
}
