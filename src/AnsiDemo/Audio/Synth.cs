using AnsiDemo.Contracts;

namespace AnsiDemo.Audio;

/// <summary>Four chip channels mixed to mono: 0 and 1 pulse, 2 triangle, 3 noise.</summary>
public sealed class Synth : ISynth
{
    private const double MixGain = 0.25;

    private readonly SynthChannel[] _channels;

    public Synth()
    {
        _channels =
        [
            new SynthChannel(SynthWaveform.Pulse),
            new SynthChannel(SynthWaveform.Pulse),
            new SynthChannel(SynthWaveform.Triangle),
            new SynthChannel(SynthWaveform.Noise),
        ];
    }

    public void NoteOn(int channel, double frequencyHz, Instrument instrument)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        ValidateChannel(channel);
        ValidateFrequency(frequencyHz);
        _channels[channel].NoteOn(frequencyHz, instrument);
    }

    public void SetFrequency(int channel, double frequencyHz)
    {
        ValidateChannel(channel);
        ValidateFrequency(frequencyHz);
        _channels[channel].SetFrequency(frequencyHz);
    }

    public void NoteOff(int channel)
    {
        ValidateChannel(channel);
        _channels[channel].NoteOff();
    }

    public void Render(Span<short> buffer)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            double mix = 0;
            foreach (var channel in _channels)
            {
                mix += channel.Sample();
            }
            buffer[i] = (short)(Math.Clamp(MixGain * mix, -1, 1) * short.MaxValue);

            foreach (var channel in _channels)
            {
                channel.Advance();
            }
        }
    }

    private static void ValidateChannel(int channel)
    {
        if (channel is < 0 or >= AudioFormat.Channels)
        {
            throw new ArgumentException(
                $"Channel must be 0..{AudioFormat.Channels - 1}, got {channel}.", nameof(channel));
        }
    }

    private static void ValidateFrequency(double frequencyHz)
    {
        // Written so that NaN is rejected as well.
        if (!(frequencyHz > 0 && frequencyHz <= AudioFormat.SampleRate / 2.0))
        {
            throw new ArgumentException(
                $"Frequency must be in (0, {AudioFormat.SampleRate / 2.0}] Hz, got {frequencyHz}.", nameof(frequencyHz));
        }
    }
}
