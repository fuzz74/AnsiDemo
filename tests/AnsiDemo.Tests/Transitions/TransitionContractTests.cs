using System.Reflection;
using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

/// <summary>5.5 and the shape every transition shares, checked for all four.</summary>
public class TransitionContractTests
{
    private const int Cols = 13;
    private const int Rows = 5;

    private static readonly Dictionary<string, Type> Types = new()
    {
        ["crossfade"] = typeof(CrossfadeTransition),
        ["wipe"] = typeof(WipeTransition),
        ["dissolve"] = typeof(DissolveTransition),
        ["push"] = typeof(PushTransition),
    };

    public static TheoryData<string> Names => new(Types.Keys);

    [Theory]
    [MemberData(nameof(Names))]
    public void CarriesItsAttributeName(string name)
    {
        Type type = Types[name];
        TransitionAttribute? attribute = type.GetCustomAttribute<TransitionAttribute>();
        Assert.NotNull(attribute);
        Assert.Equal(name, attribute.Name);
        Assert.True(type.IsPublic && type.IsSealed);
        Assert.True(typeof(ITransition).IsAssignableFrom(type));
        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IsStateless(string name)
    {
        Assert.Empty(Types[name].GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
    }

    [Theory]
    [InlineData("crossfade", 0.0)]
    [InlineData("wipe", 0.0)]
    [InlineData("dissolve", 0.0)]
    [InlineData("push", 0.0)]
    [InlineData("crossfade", -0.5)]
    [InlineData("wipe", -0.5)]
    [InlineData("dissolve", -0.5)]
    [InlineData("push", -0.5)]
    [InlineData("crossfade", double.NegativeInfinity)]
    [InlineData("wipe", double.NegativeInfinity)]
    [InlineData("dissolve", double.NegativeInfinity)]
    [InlineData("push", double.NegativeInfinity)]
    [InlineData("crossfade", double.NaN)]
    [InlineData("wipe", double.NaN)]
    [InlineData("dissolve", double.NaN)]
    [InlineData("push", double.NaN)]
    public void OutputsExactlyFromAtZeroAndBelow(string name, double progress)
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        Create(name).Apply(from, TestFrames.To(Cols, Rows), progress, output);

        TestFrames.AssertSame(from, output);
    }

    [Theory]
    [InlineData("crossfade", 1.0)]
    [InlineData("wipe", 1.0)]
    [InlineData("dissolve", 1.0)]
    [InlineData("push", 1.0)]
    [InlineData("crossfade", 1.5)]
    [InlineData("wipe", 1.5)]
    [InlineData("dissolve", 1.5)]
    [InlineData("push", 1.5)]
    [InlineData("crossfade", double.PositiveInfinity)]
    [InlineData("wipe", double.PositiveInfinity)]
    [InlineData("dissolve", double.PositiveInfinity)]
    [InlineData("push", double.PositiveInfinity)]
    public void OutputsExactlyToAtOneAndAbove(string name, double progress)
    {
        Frame to = TestFrames.To(Cols, Rows);
        Frame output = TestFrames.Output(Cols, Rows);

        Create(name).Apply(TestFrames.From(Cols, Rows), to, progress, output);

        TestFrames.AssertSame(to, output);
    }

    [Theory]
    [InlineData("crossfade", 0.3)]
    [InlineData("wipe", 0.3)]
    [InlineData("dissolve", 0.3)]
    [InlineData("push", 0.3)]
    [InlineData("crossfade", 0.8)]
    [InlineData("wipe", 0.8)]
    [InlineData("dissolve", 0.8)]
    [InlineData("push", 0.8)]
    public void LeavesItsInputsUntouched(string name, double progress)
    {
        Frame from = TestFrames.From(Cols, Rows);
        Frame to = TestFrames.To(Cols, Rows);

        Create(name).Apply(from, to, progress, TestFrames.Output(Cols, Rows));

        TestFrames.AssertSame(TestFrames.From(Cols, Rows), from);
        TestFrames.AssertSame(TestFrames.To(Cols, Rows), to);
    }

    [Theory]
    [InlineData("crossfade", "from")]
    [InlineData("wipe", "from")]
    [InlineData("dissolve", "from")]
    [InlineData("push", "from")]
    [InlineData("crossfade", "to")]
    [InlineData("wipe", "to")]
    [InlineData("dissolve", "to")]
    [InlineData("push", "to")]
    [InlineData("crossfade", "output")]
    [InlineData("wipe", "output")]
    [InlineData("dissolve", "output")]
    [InlineData("push", "output")]
    public void ThrowsWhenOneFrameDiffersInSize(string name, string odd)
    {
        ITransition transition = Create(name);
        foreach ((int cols, int rows) in new[] { (Cols + 1, Rows), (Cols, Rows - 1) })
        {
            Frame from = odd == "from" ? TestFrames.From(cols, rows) : TestFrames.From(Cols, Rows);
            Frame to = odd == "to" ? TestFrames.To(cols, rows) : TestFrames.To(Cols, Rows);
            Frame output = odd == "output" ? TestFrames.Output(cols, rows) : TestFrames.Output(Cols, Rows);
            Frame before = TestFrames.Output(output.Cols, output.Rows);

            Assert.Throws<ArgumentException>(() => transition.Apply(from, to, 0.5, output));
            TestFrames.AssertSame(before, output);
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void ApplyDoesNotAllocate(string name)
    {
        ITransition transition = Create(name);
        Frame from = TestFrames.From(80, 24);
        Frame to = TestFrames.To(80, 24);
        Frame output = TestFrames.Output(80, 24);
        transition.Apply(from, to, 0.5, output);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i <= 10; i++)
        {
            transition.Apply(from, to, i / 10.0, output);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    private static ITransition Create(string name) => (ITransition)Activator.CreateInstance(Types[name])!;
}
