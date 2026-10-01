namespace AnsiDemo.Tracker;

/// <summary>Note names and MIDI numbers in the tracker's notation ("C-4", "C#4").</summary>
public static class Notes
{
    /// <summary>Equal temperament with A-4 (MIDI 69) at 440 Hz.</summary>
    public static double Frequency(int midi) => 440.0 * Math.Pow(2.0, (midi - 69) / 12.0);

    /// <summary>Parses exactly three characters <c>[A-G](#|-)[0-8]</c>; C-4 = 60.</summary>
    /// <exception cref="FormatException">The text is not a note.</exception>
    public static int Parse(string note)
    {
        if (note is not { Length: 3 })
        {
            throw BadNote(note);
        }

        int semitone = note[0] switch
        {
            'C' => 0,
            'D' => 2,
            'E' => 4,
            'F' => 5,
            'G' => 7,
            'A' => 9,
            'B' => 11,
            _ => throw BadNote(note),
        };
        int sharp = note[1] switch
        {
            '-' => 0,
            '#' => 1,
            _ => throw BadNote(note),
        };
        int octave = note[2] - '0';
        if (octave is < 0 or > 8)
        {
            throw BadNote(note);
        }
        return 12 * (octave + 1) + semitone + sharp;
    }

    private static FormatException BadNote(string? note) =>
        new($"Bad note '{note}': expected a letter A-G, '#' or '-', and an octave 0-8, like C-4 or C#4.");
}
