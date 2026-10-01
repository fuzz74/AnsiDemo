using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class DemoRandomTests
{
    [Fact]
    public void SeedOneMatchesVectors()
    {
        var rng = new DemoRandom(1);
        Assert.Equal(270369u, rng.NextUInt());
        Assert.Equal(67634689u, rng.NextUInt());
        Assert.Equal(2647435461u, rng.NextUInt());
    }

    [Fact]
    public void SeedZeroBehavesLikeSeedOne()
    {
        var zero = new DemoRandom(0);
        var one = new DemoRandom(1);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(one.NextUInt(), zero.NextUInt());
        }
    }

    [Theory]
    [InlineData(0, 0, 0u)]
    [InlineData(1, 0, 2182377942u)]
    [InlineData(3, 7, 1728803547u)]
    public void HashMatchesVectors(int x, int y, uint expected)
    {
        Assert.Equal(expected, DemoRandom.Hash(x, y));
    }

    [Fact]
    public void NextDoubleIsNextUIntScaledIntoUnitRange()
    {
        var reference = new DemoRandom(42);
        var rng = new DemoRandom(42);
        for (int i = 0; i < 1000; i++)
        {
            double d = rng.NextDouble();
            Assert.InRange(d, 0.0, 1.0);
            Assert.True(d < 1.0);
            Assert.Equal(reference.NextUInt() / 4294967296.0, d);
        }
    }

    [Fact]
    public void NextStaysBelowMaxAndCoversRange()
    {
        var rng = new DemoRandom(7);
        var seen = new bool[10];
        for (int i = 0; i < 1000; i++)
        {
            int n = rng.Next(10);
            Assert.InRange(n, 0, 9);
            seen[n] = true;
        }
        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextIsNextDoubleTimesMax()
    {
        var reference = new DemoRandom(1);
        var rng = new DemoRandom(1);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal((int)(reference.NextDouble() * 37), rng.Next(37));
        }
    }

    [Fact]
    public void SameSeedGivesSameSequence()
    {
        var a = new DemoRandom(12345);
        var b = new DemoRandom(12345);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }
}
