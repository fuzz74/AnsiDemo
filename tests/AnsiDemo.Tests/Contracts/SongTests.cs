using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Contracts;

public class SongTests
{
    private static Pattern MakePattern(string name, int rows)
    {
        var row = new RowCell[] { new RowCell.Note(60, 0, null, 0, 0), new RowCell.Empty(), new RowCell.Off(), new RowCell.Empty() };
        return new Pattern(name, Enumerable.Range(0, rows).Select(_ => (IReadOnlyList<RowCell>)row).ToList());
    }

    private static Song MakeSong() => new(
        125, 4,
        [new Instrument("lead", 0.5, 0.8, 1.0)],
        [MakePattern("a", 16), MakePattern("b", 8)],
        [0, 1, 0, 1, 1]);

    [Fact]
    public void SecondsPerRowFollowsBpmAndRowsPerBeat()
    {
        Assert.Equal(0.12, MakeSong().SecondsPerRow, 12);
        Assert.Equal(0.25, (MakeSong() with { Bpm = 120, RowsPerBeat = 2 }).SecondsPerRow, 12);
    }

    [Fact]
    public void TotalRowsSumsPatternsInOrder()
    {
        Assert.Equal(16 + 8 + 16 + 8 + 8, MakeSong().TotalRows);
    }

    [Fact]
    public void DurationSecondsIsTotalRowsTimesSecondsPerRow()
    {
        Assert.Equal(56 * 0.12, MakeSong().DurationSeconds, 9);
    }

    [Fact]
    public void EmptyOrderHasZeroDuration()
    {
        var song = MakeSong() with { Order = [] };
        Assert.Equal(0, song.TotalRows);
        Assert.Equal(0.0, song.DurationSeconds);
    }

    [Fact]
    public void ExceptionsCarryTheirDetails()
    {
        var format = new SongFormatException("bad note", 12);
        Assert.Equal("bad note", format.Message);
        Assert.Equal(12, format.Line);

        var device = new AudioDeviceException("no device", 2);
        Assert.Equal("no device", device.Message);
        Assert.Equal(2, device.Code);
    }
}
