using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>The transitions marked with <see cref="TransitionAttribute"/>, found by reflection.</summary>
public static class TransitionCatalog
{
    private static readonly AttributedTypes<TransitionAttribute> Transitions = new(attribute => attribute.Name, "transition");

    /// <summary>Every transition name, sorted ordinally.</summary>
    public static IReadOnlyList<string> Names => Transitions.Names;

    /// <summary>A new instance of the named transition; <see cref="ArgumentException"/> for an unknown name.</summary>
    public static ITransition Create(string name) => Transitions.Create<ITransition>(name);
}
