using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>Scene positions on the bar grid of <see cref="DemoClock"/>.</summary>
public static class Timeline
{
    /// <summary>Start of scene <paramref name="index"/> in seconds; index = scenes.Count gives the end of the last scene.</summary>
    public static double StartOf(IReadOnlyList<SceneSpec> scenes, int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, scenes.Count);
        double start = 0;
        for (int i = 0; i < index; i++)
        {
            start += Length(scenes[i]);
        }
        return start;
    }

    public static double TotalSeconds(IReadOnlyList<SceneSpec> scenes) => StartOf(scenes, scenes.Count);

    /// <summary>The scene playing at <paramref name="seconds"/> and its transition state, or null outside the demo.</summary>
    public static SceneCue? CueAt(IReadOnlyList<SceneSpec> scenes, double seconds)
    {
        if (!(seconds >= 0))
        {
            return null;
        }

        double previousStart = 0, start = 0;
        for (int i = 0; i < scenes.Count; i++)
        {
            double end = start + Length(scenes[i]);
            if (seconds < end)
            {
                double sceneTime = seconds - start;
                double transitionLength = scenes[i].TransitionBars * DemoClock.SecondsPerBar;
                if (sceneTime < transitionLength)
                {
                    return i == 0
                        ? new SceneCue(i, sceneTime, -1, 0, sceneTime / transitionLength)
                        : new SceneCue(i, sceneTime, i - 1, seconds - previousStart, sceneTime / transitionLength);
                }
                return new SceneCue(i, sceneTime, null, 0, 1);
            }
            previousStart = start;
            start = end;
        }
        return null;
    }

    private static double Length(SceneSpec scene) => scene.Bars * DemoClock.SecondsPerBar;
}
