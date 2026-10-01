using AnsiDemo.Contracts;
using AnsiDemo.Core;

namespace AnsiDemo.Tests.Core;

public class CatalogTests
{
    [Fact]
    public void EffectNamesContainAttributedTestEffects()
    {
        Assert.Contains("test-pixel", EffectCatalog.Names);
        Assert.Contains("test-cell", EffectCatalog.Names);
    }

    [Fact]
    public void TransitionNamesContainAttributedTestTransition()
    {
        Assert.Contains("test-cut", TransitionCatalog.Names);
    }

    [Fact]
    public void NamesAreSortedOrdinally()
    {
        IReadOnlyList<string> effects = EffectCatalog.Names;
        IReadOnlyList<string> transitions = TransitionCatalog.Names;
        Assert.Equal(effects.Order(StringComparer.Ordinal), effects);
        Assert.Equal(transitions.Order(StringComparer.Ordinal), transitions);
    }

    [Fact]
    public void EffectCreateReturnsANewInstanceOfTheAttributedClass()
    {
        IEffect first = EffectCatalog.Create("test-pixel");
        IEffect second = EffectCatalog.Create("test-pixel");
        Assert.IsType<TestPixelEffect>(first);
        Assert.IsType<TestPixelEffect>(second);
        Assert.NotSame(first, second);
        Assert.IsType<TestCellEffect>(EffectCatalog.Create("test-cell"));
    }

    [Fact]
    public void TransitionCreateReturnsANewInstanceOfTheAttributedClass()
    {
        ITransition first = TransitionCatalog.Create("test-cut");
        ITransition second = TransitionCatalog.Create("test-cut");
        Assert.IsType<TestCutTransition>(first);
        Assert.IsType<TestCutTransition>(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void PreferredModeComesFromTheAttribute()
    {
        Assert.Equal(PixelMode.Braille, EffectCatalog.PreferredMode("test-pixel"));
        Assert.Equal(PixelMode.HalfBlock, EffectCatalog.PreferredMode("test-cell"));
    }

    [Fact]
    public void UnknownEffectThrowsNamingIt()
    {
        var create = Assert.Throws<ArgumentException>(() => EffectCatalog.Create("no-such-effect"));
        Assert.Contains("no-such-effect", create.Message);
        var mode = Assert.Throws<ArgumentException>(() => EffectCatalog.PreferredMode("no-such-effect"));
        Assert.Contains("no-such-effect", mode.Message);
    }

    [Fact]
    public void UnknownTransitionThrowsNamingIt()
    {
        var create = Assert.Throws<ArgumentException>(() => TransitionCatalog.Create("no-such-transition"));
        Assert.Contains("no-such-transition", create.Message);
    }

    [Fact]
    public void NamesAreCaseSensitive()
    {
        Assert.Throws<ArgumentException>(() => EffectCatalog.Create("TEST-PIXEL"));
        Assert.Throws<ArgumentException>(() => TransitionCatalog.Create("TEST-CUT"));
    }
}
