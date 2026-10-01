using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class SceneSpecTests
{
    [Fact]
    public void SoloSceneHasOneEffectAndNoTransition()
    {
        var scene = SceneSpec.Solo("plasma", PixelMode.HalfBlock);
        Assert.Equal("plasma", scene.Name);
        Assert.Equal(PixelMode.HalfBlock, scene.Mode);
        Assert.Equal(["plasma"], scene.Effects);
        Assert.Equal(1000, scene.Bars);
        Assert.Equal("crossfade", scene.Transition);
        Assert.Equal(0, scene.TransitionBars);
    }
}
