using AnsiDemo.Contracts;

namespace AnsiDemo.Transitions;

/// <summary>Blends the colours of every cell; the character switches from <c>from</c> to <c>to</c> at progress 0.5 (5.1).</summary>
[Transition("crossfade")]
public sealed class CrossfadeTransition : ITransition
{
    public void Apply(Frame from, Frame to, double progress, Frame output)
    {
        TransitionInput.RequireSameSize(from, to, output);
        progress = TransitionInput.Clamp(progress);

        bool toChar = progress >= 0.5;
        for (int y = 0; y < output.Rows; y++)
        {
            for (int x = 0; x < output.Cols; x++)
            {
                Cell a = from[x, y];
                Cell b = to[x, y];
                output[x, y] = new Cell(
                    toChar ? b.Ch : a.Ch,
                    Rgb.Lerp(a.Fg, b.Fg, progress),
                    Rgb.Lerp(a.Bg, b.Bg, progress));
            }
        }
    }
}
