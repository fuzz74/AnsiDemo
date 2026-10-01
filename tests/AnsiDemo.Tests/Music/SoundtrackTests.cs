using AnsiDemo.Audio;
using AnsiDemo.Contracts;
using AnsiDemo.Music;
using AnsiDemo.Tracker;

namespace AnsiDemo.Tests.Music;

public class SoundtrackTests
{
    private const int ChunkSamples = 2048;

    private static Song Parse() => SongParser.Parse(Soundtrack.Text);

    private static IEnumerable<RowCell> OrderCells(Song song, int channel) =>
        song.Order.SelectMany(p => song.Patterns[p].Rows).Select(row => row[channel]);

    [Fact]
    public void TextParses()
    {
        Song song = Parse();

        Assert.NotEmpty(song.Instruments);
        Assert.NotEmpty(song.Order);
    }

    [Fact]
    public void TempoMatchesTheDemoClock()
    {
        Song song = Parse();

        Assert.Equal(125, song.Bpm);
        Assert.Equal(4, song.RowsPerBeat);
        Assert.Equal(DemoClock.Bpm, song.Bpm);
    }

    [Fact]
    public void SongLastsAtLeastAsLongAsTheDemo()
    {
        Song song = Parse();

        Assert.True(song.DurationSeconds >= DemoClock.DemoBars * DemoClock.SecondsPerBar,
            $"Song lasts {song.DurationSeconds} s, the demo {DemoClock.DemoBars * DemoClock.SecondsPerBar} s.");
    }

    [Fact]
    public void EveryPatternInTheOrderIsTwoBarsSoSectionsChangeOnSceneBoundaries()
    {
        Song song = Parse();
        int twoBars = 2 * DemoClock.BeatsPerBar * song.RowsPerBeat;

        Assert.All(song.Order, p => Assert.Equal(twoBars, song.Patterns[p].Rows.Count));
    }

    [Fact]
    public void OrderUsesAtLeastSixDistinctPatterns()
    {
        Song song = Parse();

        Assert.True(song.Order.Distinct().Count() >= 6, $"Only {song.Order.Distinct().Count()} distinct patterns.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryChannelPlaysAtLeastHundredNotes(int channel)
    {
        Song song = Parse();

        int notes = OrderCells(song, channel).Count(cell => cell is RowCell.Note);

        Assert.True(notes >= 100, $"Channel {channel} has {notes} note cells.");
    }

    [Fact]
    public void UsesVolumeAndArpeggio()
    {
        Song song = Parse();
        var notes = Enumerable.Range(0, AudioFormat.Channels).SelectMany(ch => OrderCells(song, ch)).OfType<RowCell.Note>().ToList();

        Assert.Contains(notes, n => n.Volume is not null);
        Assert.Contains(notes, n => n.ArpX != 0 || n.ArpY != 0);
    }

    [Fact]
    public void RenderedThroughTheSynthIsLoudEnoughAndRarelyClips()
    {
        Song song = Parse();
        var player = new SongPlayer(song, new Synth(), loop: false);
        long total = (long)Math.Ceiling(player.DurationSeconds * AudioFormat.SampleRate);
        var buffer = new short[ChunkSamples];
        long clipped = 0;
        int peak = 0;

        for (long done = 0; done < total; done += ChunkSamples)
        {
            Span<short> chunk = buffer.AsSpan(0, (int)Math.Min(ChunkSamples, total - done));
            player.Render(chunk);
            foreach (short s in chunk)
            {
                int magnitude = Math.Abs((int)s);
                peak = Math.Max(peak, magnitude);
                if (magnitude >= short.MaxValue)
                {
                    clipped++;
                }
            }
        }

        Assert.Equal(player.DurationSeconds, player.PositionSeconds, 6);
        Assert.True(clipped * 100 < total, $"{clipped} of {total} samples are at full scale.");
        Assert.True(peak >= 8000, $"Peak is {peak}.");
    }
}
