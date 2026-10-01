using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Base of the recording test effects: registers itself in <see cref="TestLog"/> and keeps every context it gets.</summary>
public abstract class RecordingEffect : IEffect
{
    protected RecordingEffect()
    {
        Id = TestLog.Effects.Count;
        TestLog.Effects.Add(this);
    }

    public int Id { get; }

    public List<FrameContext> Resets { get; } = [];

    /// <summary>The first value of each generator passed to Reset, which identifies its seed.</summary>
    public List<uint> ResetRandoms { get; } = [];

    public List<FrameContext> Updates { get; } = [];

    public void Reset(FrameContext ctx, DemoRandom rng)
    {
        Resets.Add(ctx);
        ResetRandoms.Add(rng.NextUInt());
    }

    public void Update(FrameContext ctx) => Updates.Add(ctx);

    protected static string Format(Rgb color) => $"{color.R},{color.G},{color.B}";
}
