namespace AnsiDemo.Contracts;

public static class AudioFormat
{
    public const int SampleRate = 44100, Channels = 4;
}

/// <param name="Duty">Pulse channels only.</param>
public sealed record Instrument(string Name, double Duty, double Volume, double DecayPerSecond);

public interface ISampleSource
{
    /// <summary>Mono 16-bit at SampleRate; fills the whole buffer.</summary>
    void Render(Span<short> buffer);
}

public interface ISynth : ISampleSource
{
    void NoteOn(int channel, double frequencyHz, Instrument instrument);
    void SetFrequency(int channel, double frequencyHz);
    void NoteOff(int channel);
}

public interface IAudioSink : IDisposable
{
    /// <summary>AudioDeviceException when no device; InvalidOperationException if already started.</summary>
    void Start(ISampleSource source);

    /// <summary>Idempotent.</summary>
    void Stop();
}

public sealed class AudioDeviceException(string message, int code) : Exception(message)
{
    public int Code { get; } = code;
}
