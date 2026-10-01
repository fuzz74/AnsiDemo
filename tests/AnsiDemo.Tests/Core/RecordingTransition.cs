using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Runner test transition: logs its inputs, then outputs <c>to</c> with <see cref="Marker"/> at (0, 0).</summary>
[Transition("test-rec")]
public sealed class RecordingTransition : ITransition
{
    public static readonly Cell Marker = new('*', Rgb.White, Rgb.Black);

    public void Apply(Frame from, Frame to, double progress, Frame output)
    {
        TestLog.Transitions.Add(new TransitionCall(this, from.ToAnsi(), to.ToAnsi(), progress));
        output.CopyFrom(to);
        output[0, 0] = Marker;
    }
}
