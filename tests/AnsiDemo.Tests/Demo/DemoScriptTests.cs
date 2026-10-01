using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class DemoScriptTests
{
    [Theory]
    [InlineData(0, "boot", PixelMode.Braille, "metaballs matrix", 8, "crossfade", 1)]
    [InlineData(1, "title", PixelMode.HalfBlock, "plasma scroller", 10, "dissolve", 1)]
    [InlineData(2, "tunnel", PixelMode.HalfBlock, "tunnel cube", 10, "wipe", 1)]
    [InlineData(3, "fire", PixelMode.HalfBlock, "fire", 8, "crossfade", 1)]
    [InlineData(4, "roto", PixelMode.HalfBlock, "rotozoomer", 8, "push", 1)]
    [InlineData(5, "flight", PixelMode.HalfBlock, "starfield voxel", 10, "dissolve", 1)]
    [InlineData(6, "mandel", PixelMode.HalfBlock, "mandelbrot", 10, "crossfade", 1)]
    [InlineData(7, "spheres", PixelMode.Braille, "raymarch", 10, "wipe", 1)]
    public void SceneMatchesTheSceneTable(int index, string name, PixelMode mode, string effects, int bars,
                                          string transition, int transitionBars)
    {
        SceneSpec scene = DemoScript.Scenes[index];
        Assert.Equal(name, scene.Name);
        Assert.Equal(mode, scene.Mode);
        Assert.Equal(effects.Split(' '), scene.Effects);
        Assert.Equal(bars, scene.Bars);
        Assert.Equal(transition, scene.Transition);
        Assert.Equal(transitionBars, scene.TransitionBars);
    }

    [Fact]
    public void ScriptHasExactlyTheEightScenesOfTheTable()
    {
        Assert.Equal(
            ["boot", "title", "tunnel", "fire", "roto", "flight", "mandel", "spheres"],
            DemoScript.Scenes.Select(scene => scene.Name));
    }

    [Fact]
    public void BarsTotalTheDemoLength()
    {
        Assert.Equal(74, DemoClock.DemoBars);
        Assert.Equal(DemoClock.DemoBars, DemoScript.Scenes.Sum(scene => scene.Bars));
        Assert.Equal(DemoClock.DemoBars * DemoClock.SecondsPerBar, Timeline.TotalSeconds(DemoScript.Scenes), 9);
    }

    [Fact]
    public void EveryTransitionIsRegistered()
    {
        Assert.All(DemoScript.Scenes, scene => Assert.Contains(scene.Transition, TransitionCatalog.Names));
    }

    [Fact]
    public void ScenesCannotBeModified()
    {
        Assert.Same(DemoScript.Scenes, DemoScript.Scenes);
        Assert.True(IsReadOnly(DemoScript.Scenes));
        Assert.All(DemoScript.Scenes, scene => Assert.True(IsReadOnly(scene.Effects)));
    }

    private static bool IsReadOnly<T>(IReadOnlyList<T> list) => list is not ICollection<T> collection || collection.IsReadOnly;
}
