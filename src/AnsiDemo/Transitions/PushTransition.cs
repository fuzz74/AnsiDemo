using AnsiDemo.Contracts;

namespace AnsiDemo.Transitions;

/// <summary>
/// Slides <c>to</c> in from the right while pushing <c>from</c> out to the left, by s = floor(progress · Cols) columns (5.4).
/// </summary>
[Transition("push")]
public sealed class PushTransition : ITransition
{
    public void Apply(Frame from, Frame to, double progress, Frame output)
    {
        TransitionInput.RequireSameSize(from, to, output);
        int cols = output.Cols;
        int shift = TransitionInput.Columns(TransitionInput.Clamp(progress), cols);
        int seam = cols - shift;

        for (int y = 0; y < output.Rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                output[x, y] = x < seam ? from[x + shift, y] : to[x - seam, y];
            }
        }
    }
}
