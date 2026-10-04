using AnsiDemo.Contracts;
using AnsiDemo.Tracker;

namespace AnsiDemo.Tests.Tracker;

public class SongPlayerTests
{
    private const int RowSamples = 5292;                    // bpm 125, 4 rows per beat: 0.12 s

    private static readonly Instrument Lead = new("lead", 0.5, 0.9, 1.0);
    private static readonly Instrument Bass = new("bass", 0.25, 0.6, 0.0);
    private static readonly RowCell E = new RowCell.Empty();
    private static readonly RowCell O = new RowCell.Off();

    private static RowCell N(int midi, int instrument = 0, int? v = null, int x = 0, int y = 0) =>
        new RowCell.Note(midi, instrument, v, x, y);

    private static IReadOnlyList<RowCell> Row(RowCell ch0, RowCell ch1, RowCell ch2, RowCell ch3) => [ch0, ch1, ch2, ch3];

    private static Song MakeSong(params IReadOnlyList<RowCell>[] rows) =>
        new(125, 4, [Lead, Bass], [new Pattern("p", rows)], [0]);

    // Rows 0-3: notes on channels 0 and 2, an off, an arpeggio on channel 1 and an empty row.
    private static Song FourRowSong() => MakeSong(
        Row(N(60), E, N(69, 1), O),
        Row(E, N(64, 0, x: 3, y: 7), E, E),
        Row(O, E, E, N(72, 1, v: 5)),
        Row(E, E, E, E));

    private static (SongPlayer Player, RecordingSynth Synth) Play(Song song, bool loop = false)
    {
        var synth = new RecordingSynth();
        return (new SongPlayer(song, synth, loop), synth);
    }

    private static void Render(SongPlayer player, int samples) => player.Render(new short[samples]);

    [Fact]
    public void FirstSampleTriggersEveryChannelOfRowZero()
    {
        var (player, synth) = Play(FourRowSong());

        Render(player, 1);

        Assert.Equal(["On 0 261.63 lead", "On 2 440.00 bass", "Off 3"], synth.Calls);
        Assert.Equal(1, synth.SamplesRendered);
    }

    [Fact]
    public void EachRowFiresOnItsFirstSampleWithArpeggioAtThirds()
    {
        var (player, synth) = Play(FourRowSong());

        Render(player, 4 * RowSamples);

        Assert.Equal(
            [
                "On 0 261.63 lead", "On 2 440.00 bass", "Off 3",
                "On 1 329.63 lead", "Freq 1 392.00", "Freq 1 493.88",
                "Off 0", "On 3 523.25 bass",
            ],
            synth.Calls);
        Assert.Equal(
            [
                0, 0, 0,
                RowSamples, RowSamples + 1764, RowSamples + 3528,
                2 * RowSamples, 2 * RowSamples,
            ],
            synth.CallSamples);
        Assert.Equal(4 * RowSamples, synth.SamplesRendered);
    }

    [Fact]
    public void VolumeDigitScalesTheInstrumentVolume()
    {
        var (player, synth) = Play(MakeSong(Row(N(60, 0, v: 9), N(60, 0, v: 3), N(60, 1, v: 0), N(60, 1))), loop: true);

        Render(player, RowSamples + 1);

        Assert.Equal(Lead with { Volume = 0.9 * 9 / 9.0 }, synth.Instruments[0]);
        Assert.Equal(Lead with { Volume = 0.9 * 3 / 9.0 }, synth.Instruments[1]);
        Assert.Equal(Bass with { Volume = 0.0 }, synth.Instruments[2]);
        Assert.Same(Bass, synth.Instruments[3]);
        // The scaled instruments are made once, not per note (Render does not allocate).
        Assert.Equal(8, synth.Instruments.Count);
        for (int i = 0; i < 4; i++)
        {
            Assert.Same(synth.Instruments[i], synth.Instruments[i + 4]);
        }
    }

    [Fact]
    public void ArpeggioEachChannelUsesItsOwnSemitones()
    {
        var (player, synth) = Play(MakeSong(Row(N(57, 0, x: 0, y: 12), E, N(45, 1, x: 15, y: 0), E)));

        Render(player, RowSamples);

        Assert.Equal(
            ["On 0 220.00 lead", "On 2 110.00 bass", "Freq 0 220.00", "Freq 2 261.63", "Freq 0 440.00", "Freq 2 110.00"],
            synth.Calls);
        Assert.Equal([0, 0, 1764, 1764, 3528, 3528], synth.CallSamples);
    }

    [Theory]
    [InlineData(RowSamples)]                                // row 1
    [InlineData(RowSamples + 1764)]                         // first arpeggio step of row 1
    [InlineData(RowSamples + 3528)]                         // second arpeggio step of row 1
    [InlineData(2 * RowSamples)]                            // row 2
    public void EventsFireExactlyOnTheirSample(int eventSample)
    {
        var (player, synth) = Play(FourRowSong());

        Render(player, eventSample - 1);
        int before = synth.Calls.Count;
        Render(player, 1);                                  // renders sample eventSample - 1
        Assert.Equal(before, synth.Calls.Count);
        Render(player, 1);                                  // the event comes before sample eventSample
        Assert.True(synth.Calls.Count > before);
        Assert.All(synth.CallSamples.Skip(before), s => Assert.Equal(eventSample, s));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(333)]
    [InlineData(1763)]
    [InlineData(2048)]
    [InlineData(5291)]
    [InlineData(5293)]
    [InlineData(9999)]
    public void AnyBufferSplitGivesTheSameCallsOnTheSameSamples(int chunk)
    {
        var (reference, expected) = Play(FourRowSong(), loop: true);
        Render(reference, 9 * RowSamples);

        var (player, synth) = Play(FourRowSong(), loop: true);
        var buffer = new short[chunk];
        for (int done = 0; done < 9 * RowSamples; done += chunk)
        {
            player.Render(buffer.AsSpan(0, Math.Min(chunk, 9 * RowSamples - done)));
        }

        Assert.Equal(expected.Calls, synth.Calls);
        Assert.Equal(expected.CallSamples, synth.CallSamples);
        Assert.Equal(9 * RowSamples, synth.SamplesRendered);
    }

    [Fact]
    public void RowStartsAreRoundedFromTheAbsoluteRowIndex()
    {
        // 130 bpm, 4 rows per beat: 5088.4615... samples per row, so lengths alternate without drift.
        Song song = MakeSong(Enumerable.Range(0, 40).Select(_ => Row(N(60), E, E, E)).ToArray()) with { Bpm = 130 };
        var (player, synth) = Play(song);

        Render(player, 41 * 5089);

        long[] expected = Enumerable.Range(0, 40)
            .Select(k => (long)Math.Round(k * song.SecondsPerRow * AudioFormat.SampleRate))
            .ToArray();
        Assert.Equal(expected, synth.CallSamples.Take(40));
    }

    [Fact]
    public void LoopStartsOverAtRowZero()
    {
        var (player, synth) = Play(MakeSong(Row(N(60), E, E, E), Row(E, N(67), E, E)), loop: true);

        Render(player, 2 * RowSamples);
        Assert.Equal(2 * RowSamples / (double)AudioFormat.SampleRate, player.PositionSeconds, 12);
        Render(player, 1);

        Assert.Equal(["On 0 261.63 lead", "On 1 392.00 lead", "On 0 261.63 lead"], synth.Calls);
        Assert.Equal([0, RowSamples, 2 * RowSamples], synth.CallSamples);
        Assert.Equal(1 / (double)AudioFormat.SampleRate, player.PositionSeconds, 12);
    }

    [Fact]
    public void AfterTheEndWithoutLoopAllChannelsGoOffOnceAndSilenceFollows()
    {
        var (player, synth) = Play(MakeSong(Row(N(60), E, E, E), Row(E, N(67), E, E)));
        short[] buffer = new short[3 * RowSamples];
        Array.Fill(buffer, (short)1234);

        player.Render(buffer);
        player.Render(buffer);

        Assert.Equal(["On 0 261.63 lead", "On 1 392.00 lead", "Off 0", "Off 1", "Off 2", "Off 3"], synth.Calls);
        Assert.Equal(2 * RowSamples, synth.SamplesRendered);
        Assert.All(buffer, s => Assert.Equal(0, s));
        Assert.Equal(0.24, player.PositionSeconds, 12);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SongWithoutRowsRendersZeros(bool loop)
    {
        var (player, synth) = Play(new Song(125, 4, [Lead], [new Pattern("p", [])], [0]), loop);
        short[] buffer = [1, 2, 3, 4];

        player.Render(buffer);

        Assert.All(buffer, s => Assert.Equal(0, s));
        Assert.Equal(0, synth.SamplesRendered);
        Assert.Equal(0, player.PositionSeconds);
        Assert.Equal(0, player.DurationSeconds);
    }

    [Fact]
    public void PositionAndDurationAreExactToTheSample()
    {
        Song song = FourRowSong();
        var (player, _) = Play(song);

        Assert.Equal(song.DurationSeconds, player.DurationSeconds);
        Assert.Equal(0, player.PositionSeconds);
        Render(player, 1001);
        Assert.Equal(1001 / 44100.0, player.PositionSeconds);
        Render(player, 3 * RowSamples);
        Assert.Equal((1001 + 3 * RowSamples) / 44100.0, player.PositionSeconds);
        Render(player, RowSamples);
        Assert.Equal(4 * RowSamples / 44100.0, player.PositionSeconds);
        Assert.Equal(player.DurationSeconds, player.PositionSeconds, 12);
    }

    [Fact]
    public void PausedWritesZerosAndDoesNotAdvance()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 100);
        int calls = synth.Calls.Count;
        short[] buffer = new short[RowSamples * 2];
        Array.Fill(buffer, (short)-7);

        player.Paused = true;
        player.Render(buffer);

        Assert.True(player.Paused);
        Assert.All(buffer, s => Assert.Equal(0, s));
        Assert.Equal(100, synth.SamplesRendered);
        Assert.Equal(calls, synth.Calls.Count);
        Assert.Equal(100 / 44100.0, player.PositionSeconds);

        player.Paused = false;
        Render(player, RowSamples - 100);
        Render(player, 1);
        Assert.Equal(RowSamples + 1, synth.SamplesRendered);
        Assert.Equal("On 1 329.63 lead", synth.Calls[calls]);
        Assert.Equal(RowSamples, synth.CallSamples[calls]);
    }

    [Fact]
    public async Task SeekAppliesAtTheNextRenderAndTurnsEveryChannelOff()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 100);
        int calls = synth.Calls.Count;

        await Task.Run(() => player.Seek(0.13));            // first row at or after 0.13 s is row 2 (0.24 s)
        Assert.Equal(calls, synth.Calls.Count);
        Render(player, 1);

        Assert.Equal(["Off 0", "Off 1", "Off 2", "Off 3", "Off 0", "On 3 523.25 bass"], synth.Calls.Skip(calls));
        Assert.Equal((2 * RowSamples + 1) / 44100.0, player.PositionSeconds);
    }

    [Fact]
    public void SeekToARowStartLandsOnThatRow()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 3 * RowSamples + 10);

        player.Seek(0.12);
        Render(player, 1);

        Assert.Equal(
            ["Off 0", "Off 1", "Off 2", "Off 3", "On 1 329.63 lead"],
            synth.Calls.Skip(synth.Calls.Count - 5));
        Assert.Equal((RowSamples + 1) / 44100.0, player.PositionSeconds);
    }

    [Fact]
    public void SeekBeforeTheStartGoesToRowZero()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 2 * RowSamples);
        int calls = synth.Calls.Count;

        player.Seek(-5);
        Render(player, 1);

        Assert.Equal(["Off 0", "Off 1", "Off 2", "Off 3", "On 0 261.63 lead", "On 2 440.00 bass", "Off 3"], synth.Calls.Skip(calls));
        Assert.Equal(1 / 44100.0, player.PositionSeconds);
    }

    [Fact]
    public void SeekPastTheEndWithoutLoopIsTheEnd()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 100);
        int calls = synth.Calls.Count;
        short[] buffer = new short[1000];
        Array.Fill(buffer, (short)5);

        player.Seek(100);
        player.Render(buffer);
        player.Render(buffer);

        Assert.Equal(["Off 0", "Off 1", "Off 2", "Off 3"], synth.Calls.Skip(calls));
        Assert.All(buffer, s => Assert.Equal(0, s));
        Assert.Equal(100, synth.SamplesRendered);
        Assert.Equal(player.DurationSeconds, player.PositionSeconds, 12);
    }

    [Fact]
    public void SeekPastTheEndWithLoopStartsOver()
    {
        var (player, synth) = Play(FourRowSong(), loop: true);
        Render(player, 100);
        int calls = synth.Calls.Count;

        player.Seek(0.48);
        Render(player, 1);

        Assert.Equal(["Off 0", "Off 1", "Off 2", "Off 3", "On 0 261.63 lead", "On 2 440.00 bass", "Off 3"], synth.Calls.Skip(calls));
        Assert.Equal(1 / 44100.0, player.PositionSeconds);
    }

    [Fact]
    public void SeekAfterTheEndPlaysAgain()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 5 * RowSamples);

        player.Seek(0.36);
        Render(player, RowSamples);

        Assert.Equal(5 * RowSamples, synth.SamplesRendered);
        Assert.Equal(
            ["Off 0", "Off 1", "Off 2", "Off 3", "Off 0", "Off 1", "Off 2", "Off 3"],
            synth.Calls.Skip(synth.Calls.Count - 8));
    }

    [Fact]
    public void SeekWhilePausedMovesWithoutRendering()
    {
        var (player, synth) = Play(FourRowSong());
        Render(player, 100);
        player.Paused = true;

        player.Seek(0.24);
        Render(player, 500);

        Assert.Equal(100, synth.SamplesRendered);
        Assert.Equal(0.24, player.PositionSeconds, 12);
    }

    [Fact]
    public void RenderBetweenEventsDoesNotAllocate()
    {
        var (player, _) = Play(MakeSong(Row(N(60), E, E, E)));
        var buffer = new short[512];
        player.Render(buffer);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 8; i++)
        {
            player.Render(buffer);
        }
        Assert.Equal(before, GC.GetAllocatedBytesForCurrentThread());
    }

    [Fact]
    public void SeekRejectsNaN()
    {
        var (player, _) = Play(FourRowSong());
        Assert.Throws<ArgumentException>(() => player.Seek(double.NaN));
    }
}
