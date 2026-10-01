using AnsiDemo.Contracts;

namespace AnsiDemo.Transitions;

/// <summary>Reveals <c>to</c> from the left edge: columns x &lt; floor(progress · Cols) show <c>to</c> (5.2).</summary>
[Transition("wipe")]
public sealed class WipeTransition : ITransition
{
    public void Apply(Frame from, Frame to, double progress, Frame output)
    {
        TransitionInput.RequireSameSize(from, to, output);
        int edge = TransitionInput.Columns(TransitionInput.Clamp(progress), output.Cols);

        for (int y = 0; y < output.Rows; y++)
        {
            for (int x = 0; x < output.Cols; x++)
            {
                output[x, y] = x < edge ? to[x, y] : from[x, y];
            }
        }
    }
}
