using System.Diagnostics;
using System.Text;
using AnsiDemo.Audio;
using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Music;
using AnsiDemo.Tracker;

namespace AnsiDemo.Shell;

/// <summary>
/// Plays the demo live in the terminal at about 30 frames/s with music, keys and window resizing, and gives the
/// terminal back as it found it however the run ends.
/// </summary>
public static class ConsoleHost
{
    private const double FrameSeconds = 1.0 / DemoClock.HeadlessFps;
    private const double MaxStepSeconds = 0.1;

    /// <summary>
    /// Plays until the demo ends (exit 0), a quit key is pressed (exit 0) or something throws (exit 1, the message on
    /// stderr after the terminal has been restored).
    /// </summary>
    public static int Run(AppOptions o)
    {
        ArgumentNullException.ThrowIfNull(o);
        string? audioError = null;
        int exitCode;
        string? failure = null;
        try
        {
            exitCode = RunSession(o, ref audioError);
        }
        catch (Exception ex)
        {
            failure = ex.Message;
            exitCode = 1;
        }

        // The session is closed by now, so these lines land on the restored screen.
        if (audioError is not null)
        {
            Console.Error.WriteLine(audioError);
        }
        if (failure is not null)
        {
            Console.Error.WriteLine(failure);
        }
        return exitCode;
    }

    private static int RunSession(AppOptions o, ref string? audioError)
    {
        using var quitRequested = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = true;
            }
            quitRequested.Cancel();
        };

        bool readKeys = !Console.IsInputRedirected;
        bool? previousTreatControlC = null;
        ConsoleSession? session = null;
        WaveOutSink? sink = null;
        Console.CancelKeyPress += onCancel;
        try
        {
            session = new ConsoleSession();
            if (readKeys)
            {
                previousTreatControlC = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;
            }

            (int cols, int rows) = PlayableSize();
            ConsolePlayback playback;
            if (o.Scene is not null || o.Effect is not null)
            {
                playback = ConsolePlayback.ForScene(SoloScene(o), cols, rows);
            }
            else
            {
                SongPlayer? song = null;
                if (!o.NoSound)
                {
                    var player = new SongPlayer(SongParser.Parse(Soundtrack.Text), new Synth(), loop: false);
                    sink = new WaveOutSink();
                    try
                    {
                        sink.Start(player);
                        song = player;
                    }
                    catch (Exception ex)
                    {
                        // A missing or failing device never stops the demo; it plays silently.
                        audioError = $"Playing without sound: {ex.Message}";
                        sink.Dispose();
                        sink = null;
                    }
                }
                playback = ConsolePlayback.ForDemo(DemoScript.Scenes, cols, rows, song, o.Loop);
            }

            return Play(session, playback, readKeys, quitRequested.Token);
        }
        finally
        {
            try
            {
                sink?.Dispose();
                Console.CancelKeyPress -= onCancel;
                if (previousTreatControlC is { } treatControlC)
                {
                    Console.TreatControlCAsInput = treatControlC;
                }
            }
            finally
            {
                session?.Dispose();
            }
        }
    }

    private static int Play(ConsoleSession session, ConsolePlayback playback, bool readKeys, CancellationToken quit)
    {
        var frame = new Frame(playback.Cols, playback.Rows);
        var output = new StringBuilder();
        var clock = Stopwatch.StartNew();
        double previous = 0;
        while (true)
        {
            double frameStart = clock.Elapsed.TotalSeconds;
            double dt = Math.Min(frameStart - previous, MaxStepSeconds);
            previous = frameStart;

            int width = Console.WindowWidth, height = Console.WindowHeight;
            bool tooSmall = ConsoleFrameWriter.IsTooSmall(width, height);
            if (quit.IsCancellationRequested || (readKeys && HandleKeys(playback, tooSmall)))
            {
                return 0;
            }

            if (!tooSmall && (width != playback.Cols || height != playback.Rows))
            {
                playback.Resize(width, height);
                frame = new Frame(width, height);
            }

            playback.Step(dt);
            if (playback.Finished)
            {
                return 0;
            }

            output.Clear();
            if (tooSmall)
            {
                ConsoleFrameWriter.AppendTooSmall(output, width, height);
            }
            else
            {
                playback.Render(frame);
                ConsoleFrameWriter.Append(output, frame);
            }
            session.Write(output);

            // Sleep away the rest of the frame; a slow frame is not caught up.
            double remaining = FrameSeconds - (clock.Elapsed.TotalSeconds - frameStart);
            if (remaining > 0)
            {
                Thread.Sleep(TimeSpan.FromSeconds(remaining));
            }
        }
    }

    /// <summary>Applies every pending key; true when one of them quits. While too small only the quit keys work.</summary>
    private static bool HandleKeys(ConsolePlayback playback, bool tooSmall)
    {
        while (Console.KeyAvailable)
        {
            ConsoleCommand command = ConsoleKeyMap.CommandFor(Console.ReadKey(intercept: true));
            if (command == ConsoleCommand.Quit)
            {
                return true;
            }
            if (tooSmall)
            {
                continue;
            }
            switch (command)
            {
                case ConsoleCommand.Pause:
                    playback.TogglePause();
                    break;
                case ConsoleCommand.NextScene:
                    playback.SkipScenes(1);
                    break;
                case ConsoleCommand.PreviousScene:
                    playback.SkipScenes(-1);
                    break;
            }
        }
        return false;
    }

    /// <summary>The window size, or the minimum size while the window is smaller (the runner is resized later).</summary>
    private static (int Cols, int Rows) PlayableSize()
    {
        int width = Console.WindowWidth, height = Console.WindowHeight;
        return ConsoleFrameWriter.IsTooSmall(width, height)
            ? (ConsoleFrameWriter.MinCols, ConsoleFrameWriter.MinRows)
            : (width, height);
    }

    private static SceneSpec SoloScene(AppOptions o)
    {
        if (o.Effect is { } effect)
        {
            return SceneSpec.Solo(effect, EffectCatalog.PreferredMode(effect));
        }
        return DemoScript.Scenes.FirstOrDefault(scene => string.Equals(scene.Name, o.Scene, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Unknown scene: {o.Scene}", nameof(o));
    }
}
