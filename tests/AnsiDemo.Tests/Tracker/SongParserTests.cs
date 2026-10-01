using AnsiDemo.Contracts;
using AnsiDemo.Tracker;

namespace AnsiDemo.Tests.Tracker;

public class SongParserTests
{
    private const string FullSong = """
        ; A small but complete song.
        bpm 125            ; tempo
        rows-per-beat 4

        instrument lead duty=0.25 vol=0.8 decay=1.5
        instrument bass duty=0.5 vol=1 decay=0
        instrument hat duty=0.125 vol=0.3 decay=12

        pattern intro
        C-4 lead v7 a47 | ---      | C-2 bass | F#6 hat   ; '#' is a sharp, not a comment
        ---             | off      | ---      | ---
            ; a comment-only line inside a pattern
        A#3 lead a0C    | E-4 lead v0 | off   | C-8 hat aFf
        end

        pattern verse_2
        G-5 bass v9     | ---      | ---      | ---
        end

        order intro verse_2
        order intro
        """;

    // Lines 1-3; the pattern used by the error cases starts at line 4.
    private const string Header = "bpm 125\nrows-per-beat 4\ninstrument lead duty=0.5 vol=1 decay=0\n";
    private const string Row = "--- | --- | --- | ---\n";

    [Fact]
    public void ParsesAFullSongInFileOrder()
    {
        Song song = SongParser.Parse(FullSong);

        Assert.Equal(125, song.Bpm);
        Assert.Equal(4, song.RowsPerBeat);
        Assert.Equal(
            [new Instrument("lead", 0.25, 0.8, 1.5), new Instrument("bass", 0.5, 1.0, 0.0), new Instrument("hat", 0.125, 0.3, 12.0)],
            song.Instruments);
        Assert.Equal(["intro", "verse_2"], song.Patterns.Select(p => p.Name));

        IReadOnlyList<IReadOnlyList<RowCell>> intro = song.Patterns[0].Rows;
        Assert.Equal(3, intro.Count);
        Assert.Equal(
            [new RowCell.Note(60, 0, 7, 4, 7), new RowCell.Empty(), new RowCell.Note(36, 1, null, 0, 0), new RowCell.Note(90, 2, null, 0, 0)],
            intro[0]);
        Assert.Equal([new RowCell.Empty(), new RowCell.Off(), new RowCell.Empty(), new RowCell.Empty()], intro[1]);
        Assert.Equal(
            [new RowCell.Note(58, 0, null, 0, 12), new RowCell.Note(64, 0, 0, 0, 0), new RowCell.Off(), new RowCell.Note(108, 2, null, 15, 15)],
            intro[2]);

        Assert.Equal([new RowCell.Note(79, 1, 9, 0, 0), new RowCell.Empty(), new RowCell.Empty(), new RowCell.Empty()],
            Assert.Single(song.Patterns[1].Rows));

        Assert.Equal([0, 1, 0], song.Order);
        Assert.Equal(7, song.TotalRows);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void AcceptsBothLineEndings(string newline)
    {
        Song expected = SongParser.Parse(FullSong.ReplaceLineEndings("\n"));
        Song actual = SongParser.Parse(FullSong.ReplaceLineEndings(newline));

        Assert.Equal(expected.Instruments, actual.Instruments);
        Assert.Equal(expected.Order, actual.Order);
        Assert.Equal(expected.Patterns.Count, actual.Patterns.Count);
        for (int p = 0; p < expected.Patterns.Count; p++)
        {
            Assert.Equal(expected.Patterns[p].Name, actual.Patterns[p].Name);
            Assert.Equal(expected.Patterns[p].Rows, actual.Patterns[p].Rows);
        }
    }

    [Fact]
    public void AcceptsAnEmptyPatternAndTabs()
    {
        Song song = SongParser.Parse("bpm\t140\nrows-per-beat 2\npattern silent\nend\norder silent silent\n");

        Assert.Equal(140, song.Bpm);
        Assert.Empty(song.Instruments);
        Assert.Empty(Assert.Single(song.Patterns).Rows);
        Assert.Equal([0, 0], song.Order);
    }

    [Theory]
    [InlineData(Header + "tempo 120\n", 4)]
    [InlineData("Bpm 125\n", 1)]
    public void UnknownDirective(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\nC-4 bass | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | --- | --- | C-4 Lead\nend\norder p\n", 5)]
    public void UnknownInstrument(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\n" + Row + "end\norder p\norder p q\n", 8)]
    [InlineData(Header + "order p\n", 4)]
    public void UnknownPattern(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\n" + Row + "--- | --- | ---\nend\norder p\n", 6)]
    [InlineData(Header + "pattern p\n--- | --- | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n---\nend\norder p\n", 5)]
    public void RowWithoutFourCells(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\nH-4 lead | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | C-9 lead | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | --- | C4 lead | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | --- | --- | C-4\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | --- | --- | -- \nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\n--- | --- | | ---\nend\norder p\n", 5)]
    public void BadNote(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData("bpm abc\nrows-per-beat 4\n", 1)]
    [InlineData("bpm 0\nrows-per-beat 4\n", 1)]
    [InlineData("bpm 12.5\nrows-per-beat 4\n", 1)]
    [InlineData("bpm\nrows-per-beat 4\n", 1)]
    [InlineData("bpm 125 130\nrows-per-beat 4\n", 1)]
    [InlineData("bpm 125\nrows-per-beat -4\n", 2)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=1.5 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=-0.1 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=1 decay=-1\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=x decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=1,0 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=1 decay=Infinity\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a vol=1 duty=0.5 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.5 vol=1\n", 3)]
    [InlineData(Header + "pattern p\nC-4 lead v10 | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\nC-4 lead vA | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\nC-4 lead a4 | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\nC-4 lead aG1 | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\nC-4 lead a47 v7 | --- | --- | ---\nend\norder p\n", 5)]
    [InlineData(Header + "pattern p\nC-4 lead v7 a47 x | --- | --- | ---\nend\norder p\n", 5)]
    public void BadValue(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=0.3 vol=1 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=1 vol=1 decay=0\n", 3)]
    [InlineData("bpm 125\nrows-per-beat 4\ninstrument a duty=half vol=1 decay=0\n", 3)]
    public void BadDuty(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData("rows-per-beat 4\npattern p\n" + Row + "end\norder p\n", 5)]
    [InlineData("rows-per-beat 4\npattern p\n" + Row + "end\norder p\n; the end\n\n", 7)]
    [InlineData("", 1)]
    public void MissingBpm(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData("bpm 125\npattern p\n" + Row + "end\norder p", 5)]
    public void MissingRowsPerBeat(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\n" + Row + "end\n", 6)]
    [InlineData(Header + "pattern p\n" + Row + "end\r\n\r\n; no order\r\n", 8)]
    [InlineData("bpm 125\r\nrows-per-beat 4", 2)]
    public void MissingOrder(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "instrument lead duty=0.25 vol=1 decay=0\n", 4)]
    [InlineData(Header + "pattern p\n" + Row + "end\npattern p\n" + Row + "end\norder p\n", 7)]
    public void DuplicateName(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData("instrument my-lead duty=0.5 vol=1 decay=0\n", 1)]
    [InlineData(Header + "pattern p.1\n" + Row + "end\norder p.1\n", 4)]
    [InlineData(Header + "pattern\n" + Row + "end\n", 4)]
    [InlineData(Header + "pattern a b\n" + Row + "end\n", 4)]
    [InlineData(Header + "pattern p\n" + Row + "end\norder\n", 7)]
    public void BadName(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\n" + Row, 5)]
    [InlineData(Header + "pattern p\n" + Row + "\n; still open\n", 7)]
    [InlineData(Header + "pattern p\n" + Row + "end\npattern q\n" + Row + Row, 9)]
    public void PatternWithoutEnd(string text, int line) => AssertFails(text, line);

    [Theory]
    [InlineData(Header + "pattern p\n" + Row + "end\nbpm 120\norder p\n", 7)]
    [InlineData(Header + "pattern p\n" + Row + "end\nrows-per-beat 2\norder p\n", 7)]
    [InlineData(Header + "pattern p\n" + Row + "end\ninstrument b duty=0.5 vol=1 decay=0\norder p\n", 7)]
    [InlineData(Header + "pattern p\n" + Row + "end\norder p\npattern q\n" + Row + "end\n", 8)]
    [InlineData("bpm 125\nbpm 125\n", 2)]
    [InlineData(Header + "rows-per-beat 4\n", 4)]
    [InlineData(Header + "end\n", 4)]
    public void DirectiveInTheWrongPlaceOrRepeated(string text, int line) => AssertFails(text, line);

    [Fact]
    public void RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SongParser.Parse(null!));
    }

    private static void AssertFails(string text, int line)
    {
        SongFormatException e = Assert.Throws<SongFormatException>(() => SongParser.Parse(text));
        Assert.Equal(line, e.Line);
        Assert.StartsWith($"Line {line}: ", e.Message);
    }
}
