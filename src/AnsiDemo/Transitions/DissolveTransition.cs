using AnsiDemo.Contracts;

namespace AnsiDemo.Transitions;

/// <summary>Switches cells to <c>to</c> in a fixed pseudo-random order given by <see cref="DemoRandom.Hash"/> (5.3).</summary>
[Transition("dissolve")]
public sealed class DissolveTransition : ITransition
{
    public void Apply(Frame from, Frame to, double progress, Frame output)
    {
        TransitionInput.RequireSameSize(from, to, output);
        progress = TransitionInput.Clamp(progress);

        for (int y = 0; y < output.Rows; y++)
        {
            for (int x = 0; x < output.Cols; x++)
            {
                output[x, y] = DemoRandom.Hash(x, y) / 4294967296.0 < progress ? to[x, y] : from[x, y];
            }
        }
    }
}
