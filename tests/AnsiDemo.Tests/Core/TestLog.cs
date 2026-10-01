namespace AnsiDemo.Tests.Core;

/// <summary>
/// What the recording test effects and transition were asked to do. Test parallelization is off assembly-wide, so a
/// test clears the log, runs the code under test and reads the log afterwards.
/// </summary>
public static class TestLog
{
    /// <summary>Every recording effect, in creation order; an effect's index here is its <see cref="RecordingEffect.Id"/>.</summary>
    public static List<RecordingEffect> Effects { get; } = [];

    /// <summary>One line per Render call of a recording effect, in call order.</summary>
    public static List<string> Renders { get; } = [];

    /// <summary>One entry per Apply call of <see cref="RecordingTransition"/>, in call order.</summary>
    public static List<TransitionCall> Transitions { get; } = [];

    public static void Clear()
    {
        Effects.Clear();
        Renders.Clear();
        Transitions.Clear();
    }
}
