using AnsiDemo.Audio;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Music;
using AnsiDemo.Tracker;

namespace AnsiDemo.Shell;

/// <summary>
/// Runs the forms of the command line that need no terminal: <c>--frame</c> writes one frame as ANSI text and
/// <c>--wav</c> writes the soundtrack to a file. Never touches the console; all output goes to the given writers.
/// </summary>
public static class HeadlessHost
{
    /// <summary>
    /// Writes the WAV file or the frame and returns 0; for options it cannot run (neither <c>Frame</c> nor <c>Wav</c>,
    /// an unknown scene or effect, a time outside what is rendered) it writes the usage to <paramref name="stderr"/>,
    /// nothing to <paramref name="stdout"/>, and returns 2.
    /// </summary>
    public static int Run(AppOptions o, TextWriter stdout, TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(o);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (o.Wav is not null)
        {
            WriteWav(o.Wav);
            return 0;
        }

        Frame? frame = o.Frame is { } seconds ? RenderFrame(o, seconds) : null;
        if (frame is null)
        {
            stderr.WriteLine(AppOptionsParser.Usage);
            stderr.Flush();
            return 2;
        }

        stdout.Write(frame.ToAnsi());
        stdout.Flush();
        return 0;
    }

    private static void WriteWav(string path)
    {
        double totalSeconds = Timeline.TotalSeconds(DemoScript.Scenes);
        long totalSamples = (long)Math.Round(totalSeconds * AudioFormat.SampleRate);
        using var sink = new WavFileSink(path, totalSamples);
        sink.Start(new SongPlayer(SongParser.Parse(Soundtrack.Text), new Synth(), loop: false));
    }

    /// <summary>The frame the options ask for, or null when they name nothing renderable at that time.</summary>
    private static Frame? RenderFrame(AppOptions o, double seconds)
    {
        if (o.Cols <= 0 || o.Rows <= 0)
        {
            return null;
        }

        if (o.Scene is null && o.Effect is null)
        {
            try
            {
                return DemoRunner.RenderAt(DemoScript.Scenes, o.Cols, o.Rows, seconds);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        SceneSpec? scene = (o.Scene, o.Effect) switch
        {
            ({ } name, null) => SceneNamed(name),
            (null, { } effect) => SoloOf(effect),
            _ => null,
        };
        if (scene is null || !(seconds >= 0 && seconds < scene.Bars * DemoClock.SecondsPerBar))
        {
            return null;
        }
        return SceneRunner.RenderAt(scene, o.Cols, o.Rows, seconds);
    }

    private static SceneSpec? SceneNamed(string name) =>
        DemoScript.Scenes.FirstOrDefault(scene => string.Equals(scene.Name, name, StringComparison.Ordinal));

    private static SceneSpec? SoloOf(string effect) =>
        EffectCatalog.Names.Contains(effect, StringComparer.Ordinal)
            ? SceneSpec.Solo(effect, EffectCatalog.PreferredMode(effect))
            : null;
}
