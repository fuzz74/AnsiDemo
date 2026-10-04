using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Catalog test transition: shows <c>from</c> before the halfway point and <c>to</c> from then on.</summary>
[Transition("test-cut")]
public sealed class TestCutTransition : ITransition
{
    public void Apply(Frame from, Frame to, double progress, Frame output) => output.CopyFrom(progress < 0.5 ? from : to);
}
