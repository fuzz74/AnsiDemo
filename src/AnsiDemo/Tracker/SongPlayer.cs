using AnsiDemo.Contracts;

namespace AnsiDemo.Tracker;

/// <summary>
/// Plays a <see cref="Song"/> on an <see cref="ISynth"/>: walks the order list row by row and calls the synth
/// on the exact sample of every row start and arpeggio step, whatever the buffer sizes (spec 8.4-8.6).
/// <see cref="Render"/> runs on the audio thread; <see cref="Paused"/> and <see cref="Seek"/> may be used from another.
/// </summary>
public sealed class SongPlayer : ISampleSource
{
    private const int Channels = AudioFormat.Channels;
    private const int VolumeSteps = 10;                     // v0..v9

    // The next event of the current row.
    private const int RowStartPending = 0, FirstArpPending = 1, SecondArpPending = 2, RowDone = 3;

    private readonly ISynth synth;
    private readonly bool loop;
    private readonly IReadOnlyList<RowCell>[] rows;         // the order list flattened, one entry per song row
    private readonly bool[] rowHasArp;
    private readonly long[] rowStarts;                      // first sample of each row, then the end of the song
    private readonly Instrument[] instruments;
    private readonly Instrument[] scaledInstruments;        // [instrument · VolumeSteps + v] with Volume · v / 9

    private double pendingSeek = double.NaN;                // NaN = none; written by Seek, taken by Render
    private volatile bool paused;
    private long position;                                  // song samples played in the current pass
    private int row;                                        // current row; rows.Length at the end of the order list
    private int stage = RowStartPending;
    private bool ended;                                     // past the end without loop: channels off, writing zeros

    public SongPlayer(Song song, ISynth synth, bool loop)
    {
        ArgumentNullException.ThrowIfNull(song);
        ArgumentNullException.ThrowIfNull(synth);
        this.synth = synth;
        this.loop = loop;
        DurationSeconds = song.DurationSeconds;

        instruments = [.. song.Instruments];
        scaledInstruments = new Instrument[instruments.Length * VolumeSteps];
        for (int i = 0; i < instruments.Length; i++)
        {
            for (int v = 0; v < VolumeSteps; v++)
            {
                scaledInstruments[i * VolumeSteps + v] = instruments[i] with { Volume = instruments[i].Volume * v / 9.0 };
            }
        }

        rows = [.. song.Order.SelectMany(p => song.Patterns[p].Rows)];
        rowHasArp = new bool[rows.Length];
        for (int k = 0; k < rows.Length; k++)
        {
            if (rows[k].Count != Channels)
            {
                throw new ArgumentException($"Song row {k} has {rows[k].Count} cells, expected {Channels}.", nameof(song));
            }
            foreach (RowCell cell in rows[k])
            {
                if (cell is RowCell.Note note)
                {
                    if ((uint)note.Instrument >= (uint)instruments.Length || note.Volume is < 0 or >= VolumeSteps)
                    {
                        throw new ArgumentException($"Song row {k} has a note with a bad instrument or volume: {note}.", nameof(song));
                    }
                    rowHasArp[k] |= note.ArpX != 0 || note.ArpY != 0;
                }
            }
        }

        rowStarts = new long[rows.Length + 1];
        for (int k = 0; k <= rows.Length; k++)
        {
            rowStarts[k] = (long)Math.Round(k * song.SecondsPerRow * AudioFormat.SampleRate, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>Song samples played / SampleRate; wraps to 0 when looping.</summary>
    public double PositionSeconds => Volatile.Read(ref position) / (double)AudioFormat.SampleRate;

    public double DurationSeconds { get; }

    /// <summary>While true, <see cref="Render"/> writes zeros and does not advance.</summary>
    public bool Paused
    {
        get => paused;
        set => paused = value;
    }

    /// <summary>
    /// Takes effect at the next <see cref="Render"/>: moves to the first row starting at or after
    /// <paramref name="seconds"/> and turns every channel off.
    /// </summary>
    public void Seek(double seconds)
    {
        if (double.IsNaN(seconds))
        {
            throw new ArgumentException("Seek time must be a number.", nameof(seconds));
        }
        Interlocked.Exchange(ref pendingSeek, seconds);
    }

    public void Render(Span<short> buffer)
    {
        double seek = Interlocked.Exchange(ref pendingSeek, double.NaN);
        if (!double.IsNaN(seek))
        {
            ApplySeek(seek);
        }
        if (paused)
        {
            buffer.Clear();
            return;
        }

        int done = 0;
        while (done < buffer.Length)
        {
            long nextEvent = FireDueEvents();
            if (ended)
            {
                buffer[done..].Clear();
                return;
            }
            int count = (int)Math.Min(buffer.Length - done, nextEvent - position);
            synth.Render(buffer.Slice(done, count));
            done += count;
            Volatile.Write(ref position, position + count);
        }
    }

    /// <summary>Fires every event due at the current position and returns the sample of the next one.</summary>
    private long FireDueEvents()
    {
        while (true)
        {
            if (row == rows.Length)
            {
                if (!loop || rowStarts[^1] == 0)
                {
                    End();
                    return position;
                }
                row = 0;
                stage = RowStartPending;
                Volatile.Write(ref position, 0);
            }

            long start = rowStarts[row], length = rowStarts[row + 1] - start;
            long due = stage switch
            {
                RowStartPending => start,
                FirstArpPending => start + (long)Math.Round(length / 3.0, MidpointRounding.AwayFromZero),
                SecondArpPending => start + (long)Math.Round(2 * length / 3.0, MidpointRounding.AwayFromZero),
                _ => start + length,
            };
            if (position < due)
            {
                return due;
            }

            switch (stage)
            {
                case RowStartPending:
                    StartRow(rows[row]);
                    stage = rowHasArp[row] ? FirstArpPending : RowDone;
                    break;
                case FirstArpPending:
                    StepArpeggio(rows[row], second: false);
                    stage = SecondArpPending;
                    break;
                case SecondArpPending:
                    StepArpeggio(rows[row], second: true);
                    stage = RowDone;
                    break;
                default:
                    row++;
                    stage = RowStartPending;
                    break;
            }
        }
    }

    private void StartRow(IReadOnlyList<RowCell> cells)
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            switch (cells[ch])
            {
                case RowCell.Note note:
                    Instrument instrument = note.Volume is int v
                        ? scaledInstruments[note.Instrument * VolumeSteps + v]
                        : instruments[note.Instrument];
                    synth.NoteOn(ch, Notes.Frequency(note.Midi), instrument);
                    break;
                case RowCell.Off:
                    synth.NoteOff(ch);
                    break;
            }
        }
    }

    private void StepArpeggio(IReadOnlyList<RowCell> cells, bool second)
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            if (cells[ch] is RowCell.Note note && (note.ArpX != 0 || note.ArpY != 0))
            {
                synth.SetFrequency(ch, Notes.Frequency(note.Midi + (second ? note.ArpY : note.ArpX)));
            }
        }
    }

    private void End()
    {
        if (!ended)
        {
            ended = true;
            AllNotesOff();
        }
    }

    private void ApplySeek(double seconds)
    {
        double target = Math.Round(seconds * AudioFormat.SampleRate, MidpointRounding.AwayFromZero);
        int lo = 0, hi = rows.Length;                       // first row whose start is at or after target
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (rowStarts[mid] < target)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        AllNotesOff();
        row = lo;
        stage = RowStartPending;
        Volatile.Write(ref position, rowStarts[lo]);
        ended = lo == rows.Length && !loop;
    }

    private void AllNotesOff()
    {
        for (int ch = 0; ch < Channels; ch++)
        {
            synth.NoteOff(ch);
        }
    }
}
