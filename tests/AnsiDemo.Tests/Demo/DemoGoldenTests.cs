using AnsiDemo.Contracts;
using AnsiDemo.Core;
using AnsiDemo.Shell;
using AnsiDemo.Tests.Support;

namespace AnsiDemo.Tests.Demo;

public class DemoGoldenTests
{
    public static TheoryData<int> SceneIndexes => new(Enumerable.Range(0, DemoScript.Scenes.Count));

    /// <summary>The middle of the transition into scene i (scene 0 fades in from a blank frame).</summary>
    public static double MidTransition(int i) => Timeline.StartOf(DemoScript.Scenes, i) + 0.5 * TransitionSeconds(i);

    /// <summary>The first headless step after the transition into scene i has ended.</summary>
    public static double JustIn(int i) =>
        Timeline.StartOf(DemoScript.Scenes, i) + TransitionSeconds(i) + DemoClock.HeadlessStep;

    private static double TransitionSeconds(int i) => DemoScript.Scenes[i].TransitionBars * DemoClock.SecondsPerBar;

    [Theory]
    [MemberData(nameof(SceneIndexes))]
    public void MiddleOfTheTransitionIntoTheSceneMatchesItsGolden(int i)
    {
        Golden.Assert(DemoRunner.RenderAt(DemoScript.Scenes, 80, 24, MidTransition(i)), $"scene{i}-mid");
    }

    [Theory]
    [MemberData(nameof(SceneIndexes))]
    public void SceneJustAfterItsTransitionMatchesItsGoldenAndIsLit(int i)
    {
        Frame frame = DemoRunner.RenderAt(DemoScript.Scenes, 80, 24, JustIn(i));

        Assert.True(frame.LitCount() > 0, $"Scene {i} ({DemoScript.Scenes[i].Name}) is blank just after its transition.");
        Golden.Assert(frame, $"scene{i}-in");
    }
}
