using AnsiDemo.Contracts;

namespace AnsiDemo.Core;

/// <summary>The effects marked with <see cref="EffectAttribute"/>, found by reflection.</summary>
public static class EffectCatalog
{
    private static readonly AttributedTypes<EffectAttribute> Effects = new(attribute => attribute.Name, "effect");

    /// <summary>Every effect name, sorted ordinally.</summary>
    public static IReadOnlyList<string> Names => Effects.Names;

    /// <summary>A new instance of the named effect; <see cref="ArgumentException"/> for an unknown name.</summary>
    public static IEffect Create(string name) => Effects.Create<IEffect>(name);

    /// <summary>The pixel mode the named effect is designed for; <see cref="ArgumentException"/> for an unknown name.</summary>
    public static PixelMode PreferredMode(string name) => Effects.Find(name).Attribute.PreferredMode;
}
