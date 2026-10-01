namespace AnsiDemo.Contracts;

/// <param name="Transition">Name of the transition into this scene.</param>
public sealed record SceneSpec(string Name, PixelMode Mode, IReadOnlyList<string> Effects, int Bars,
                               string Transition, int TransitionBars)
{
    public static SceneSpec Solo(string effect, PixelMode mode) => new(effect, mode, [effect], 1000, "crossfade", 0);
}

/// <param name="FromScene">Scene transitioned from (-1 = blank frame), or null outside a transition.</param>
public sealed record SceneCue(int Scene, double SceneTime, int? FromScene, double FromSceneTime, double Progress);

[AttributeUsage(AttributeTargets.Class)]
public sealed class TransitionAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public interface ITransition
{
    void Apply(Frame from, Frame to, double progress, Frame output);
}
