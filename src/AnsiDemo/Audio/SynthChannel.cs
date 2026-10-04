using AnsiDemo.Contracts;

namespace AnsiDemo.Audio;

/// <summary>One oscillator of the <see cref="Synth"/> with its envelope; silent until the first NoteOn.</summary>
internal sealed class SynthChannel(SynthWaveform waveform)
{
    private const int NoiseStepsPerCycle = 16;

    private bool _active;
    private double _frequency;
    private double _duty;
    private double _volume;
    private double _decayPerSecond;
    private double _phase;
    private double _envelope;
    private int _lfsr;
    private double _noiseSteps;

    public void NoteOn(double frequencyHz, Instrument instrument)
    {
        _active = true;
        _frequency = frequencyHz;
        _duty = instrument.Duty;
        _volume = instrument.Volume;
        _decayPerSecond = instrument.DecayPerSecond;
        _phase = 0;
        _envelope = 1;
        _lfsr = 1;
        _noiseSteps = 0;
    }

    public void SetFrequency(double frequencyHz) => _frequency = frequencyHz;

    public void NoteOff() => _active = false;

    /// <summary>The current output scaled by volume and envelope; 0 while silent.</summary>
    public double Sample()
    {
        if (!_active)
        {
            return 0;
        }

        double value = waveform switch
        {
            SynthWaveform.Pulse => _phase < _duty ? 1 : -1,
            SynthWaveform.Triangle => _phase < 0.5 ? -1 + 4 * _phase : 3 - 4 * _phase,
            _ => (_lfsr & 1) != 0 ? 1 : -1,
        };
        return value * _volume * _envelope;
    }

    /// <summary>Moves phase, envelope and noise generator forward by one sample.</summary>
    public void Advance()
    {
        if (!_active)
        {
            return;
        }

        _phase += _frequency / AudioFormat.SampleRate;
        if (_phase >= 1)
        {
            _phase -= Math.Floor(_phase);
        }

        _envelope = Math.Max(0, _envelope - _decayPerSecond / AudioFormat.SampleRate);

        if (waveform == SynthWaveform.Noise)
        {
            _noiseSteps += NoiseStepsPerCycle * _frequency / AudioFormat.SampleRate;
            while (_noiseSteps >= 1)
            {
                int bit = (_lfsr ^ (_lfsr >> 1)) & 1;
                _lfsr = (_lfsr >> 1) | (bit << 14);
                _noiseSteps -= 1;
            }
        }
    }
}
