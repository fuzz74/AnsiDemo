namespace AnsiDemo.Contracts;

public abstract record RowCell
{
    /// <summary>"---": leave the channel as it is.</summary>
    public sealed record Empty : RowCell;

    /// <summary>"off": silence the channel.</summary>
    public sealed record Off : RowCell;

    /// <param name="Volume">0..9, or null for the instrument's volume.</param>
    /// <param name="ArpX">Arpeggio semitones at 1/3 of the row, 0..15.</param>
    /// <param name="ArpY">Arpeggio semitones at 2/3 of the row, 0..15.</param>
    public sealed record Note(int Midi, int Instrument, int? Volume, int ArpX, int ArpY) : RowCell;
}

/// <param name="Rows">Every row has 4 cells, in channel order.</param>
public sealed record Pattern(string Name, IReadOnlyList<IReadOnlyList<RowCell>> Rows);

/// <param name="Order">Indexes into Patterns.</param>
public sealed record Song(int Bpm, int RowsPerBeat, IReadOnlyList<Instrument> Instruments,
                          IReadOnlyList<Pattern> Patterns, IReadOnlyList<int> Order)
{
    public double SecondsPerRow => 60.0 / (Bpm * RowsPerBeat);
    public int TotalRows => Order.Sum(i => Patterns[i].Rows.Count);
    public double DurationSeconds => TotalRows * SecondsPerRow;
}

public sealed class SongFormatException(string message, int line) : Exception(message)
{
    public int Line { get; } = line;
}
