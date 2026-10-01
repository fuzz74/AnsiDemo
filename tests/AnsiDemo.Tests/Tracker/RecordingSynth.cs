using System.Globalization;
using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Tracker;

/// <summary>An <see cref="ISynth"/> that records its calls as text and renders silence.</summary>
public sealed class RecordingSynth : ISynth
{
    /// <summary>"On &lt;ch&gt; &lt;hz:F2&gt; &lt;instrument name&gt;", "Freq &lt;ch&gt; &lt;hz:F2&gt;" or "Off &lt;ch&gt;", in call order.</summary>
    public List<string> Calls { get; } = [];

    /// <summary>For each entry of <see cref="Calls"/>, the value of <see cref="SamplesRendered"/> when it was made.</summary>
    public List<long> CallSamples { get; } = [];

    /// <summary>The instrument of every NoteOn, in call order.</summary>
    public List<Instrument> Instruments { get; } = [];

    public long SamplesRendered { get; private set; }

    public void NoteOn(int channel, double frequencyHz, Instrument instrument)
    {
        Record(string.Create(CultureInfo.InvariantCulture, $"On {channel} {frequencyHz:F2} {instrument.Name}"));
        Instruments.Add(instrument);
    }

    public void SetFrequency(int channel, double frequencyHz) =>
        Record(string.Create(CultureInfo.InvariantCulture, $"Freq {channel} {frequencyHz:F2}"));

    public void NoteOff(int channel) => Record(string.Create(CultureInfo.InvariantCulture, $"Off {channel}"));

    public void Render(Span<short> buffer)
    {
        buffer.Clear();
        SamplesRendered += buffer.Length;
    }

    private void Record(string call)
    {
        Calls.Add(call);
        CallSamples.Add(SamplesRendered);
    }
}
