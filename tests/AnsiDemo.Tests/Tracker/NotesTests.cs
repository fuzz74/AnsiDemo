using AnsiDemo.Tracker;

namespace AnsiDemo.Tests.Tracker;

public class NotesTests
{
    [Theory]
    [InlineData(69, 440.0)]
    [InlineData(81, 880.0)]
    [InlineData(57, 220.0)]
    [InlineData(60, 261.6255653005986)]
    [InlineData(61, 277.1826309768721)]
    public void FrequencyIsEqualTemperedFromA440(int midi, double expected)
    {
        Assert.Equal(expected, Notes.Frequency(midi), 9);
    }

    [Fact]
    public void FrequencyFollowsTheFormula()
    {
        for (int midi = 0; midi <= 120; midi++)
        {
            Assert.Equal(440.0 * Math.Pow(2.0, (midi - 69) / 12.0), Notes.Frequency(midi));
        }
    }

    [Theory]
    [InlineData("C-4", 60)]
    [InlineData("C#4", 61)]
    [InlineData("B-3", 59)]
    [InlineData("A-4", 69)]
    [InlineData("D-4", 62)]
    [InlineData("E-4", 64)]
    [InlineData("F#4", 66)]
    [InlineData("G-4", 67)]
    [InlineData("A#4", 70)]
    [InlineData("C-0", 12)]
    [InlineData("B-8", 119)]
    [InlineData("G#8", 116)]
    public void ParseMapsNamesToMidi(string note, int midi)
    {
        Assert.Equal(midi, Notes.Parse(note));
    }

    [Theory]
    [InlineData("")]
    [InlineData("C4")]
    [InlineData("C-44")]
    [InlineData(" C-4")]
    [InlineData("H-4")]
    [InlineData("c-4")]
    [InlineData("Cb4")]
    [InlineData("C+4")]
    [InlineData("C-9")]
    [InlineData("C--")]
    [InlineData("---")]
    [InlineData("off")]
    public void ParseRejectsAnythingElse(string note)
    {
        Assert.Throws<FormatException>(() => Notes.Parse(note));
    }

    [Fact]
    public void ParseRejectsNull()
    {
        Assert.Throws<FormatException>(() => Notes.Parse(null!));
    }
}
