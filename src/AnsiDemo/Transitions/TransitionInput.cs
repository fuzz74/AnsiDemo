using AnsiDemo.Contracts;

namespace AnsiDemo.Transitions;

/// <summary>Argument checks shared by every transition (5.5).</summary>
internal static class TransitionInput
{
    /// <summary>Throws <see cref="ArgumentException"/> unless from, to and output have the same size.</summary>
    public static void RequireSameSize(Frame from, Frame to, Frame output)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(output);
        if (to.Cols != from.Cols || to.Rows != from.Rows)
        {
            throw new ArgumentException(
                $"Transition frame size mismatch: to is {to.Cols}x{to.Rows}, from is {from.Cols}x{from.Rows}.", nameof(to));
        }
        if (output.Cols != from.Cols || output.Rows != from.Rows)
        {
            throw new ArgumentException(
                $"Transition frame size mismatch: output is {output.Cols}x{output.Rows}, from is {from.Cols}x{from.Rows}.",
                nameof(output));
        }
    }

    /// <summary>Progress clamped to 0..1; NaN counts as 0.</summary>
    public static double Clamp(double progress) => double.IsNaN(progress) ? 0.0 : Math.Clamp(progress, 0.0, 1.0);

    /// <summary>floor(progress · cols) for a clamped progress, so 0..cols.</summary>
    public static int Columns(double progress, int cols) => (int)Math.Floor(progress * cols);
}
