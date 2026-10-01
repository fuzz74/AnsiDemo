using AnsiDemo.Audio;
using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Audio;

public class SynthTests
{
    // f / SampleRate = 1/100: one period every 100 samples.
    private const double Period100Hz = 441.0;

    // f / SampleRate = 1/64 exactly, so phases land on exact quarter points.
    private const double Period64Hz = AudioFormat.SampleRate / 64.0;

    // 16 · f / SampleRate = 1: one noise step per sample.
    private const double NoiseStepPerSampleHz = AudioFormat.SampleRate / 16.0;

    // DecayPerSecond / SampleRate = 1/64 exactly: the envelope reaches 0 after 64 samples.
    private const double DecayIn64Samples = AudioFormat.SampleRate / 64.0;

    private static Instrument Steady(double duty = 0.5, double volume = 1.0) => new("steady", duty, volume, 0);

    private static short Mix(double sum) => (short)(Math.Clamp(0.25 * sum, -1, 1) * 32767);

    private static short[] Render(Synth synth, int count)
    {
        var buffer = new short[count];
        synth.Render(buffer);
        return buffer;
    }

    private static int StepLfsr(int lfsr)
    {
        int bit = (lfsr ^ (lfsr >> 1)) & 1;
        return (lfsr >> 1) | (bit << 14);
    }

    [Fact]
    public void IsSilentBeforeAnyNoteOn()
    {
        var synth = new Synth();

        Assert.All(Render(synth, 4096), s => Assert.Equal(0, s));
    }

    [Fact]
    public void RenderOverwritesTheWholeBuffer()
    {
        var synth = new Synth();
        var buffer = new short[256];
        Array.Fill(buffer, (short)1234);

        synth.Render(buffer);

        Assert.All(buffer, s => Assert.Equal(0, s));
    }

    [Theory]
    [InlineData(0, 0.5)]
    [InlineData(0, 0.25)]
    [InlineData(1, 0.5)]
    [InlineData(1, 0.25)]
    public void PulseIsHighWhilePhaseIsBelowDuty(int channel, double duty)
    {
        var synth = new Synth();
        synth.NoteOn(channel, Period100Hz, Steady(duty, 0.8));

        short[] samples = Render(synth, 300);

        int highSamples = (int)(duty * 100);
        for (int k = 0; k < samples.Length; k++)
        {
            double value = k % 100 < highSamples ? 1 : -1;
            Assert.Equal(Mix(value * 0.8), samples[k]);
        }
    }

    [Fact]
    public void PulseIsScaledByAQuarterOfTheVolume()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady(0.5, 0.8));

        short[] samples = Render(synth, 100);

        // 0.25 · 0.8 · 32767 = 6553.4, truncated.
        Assert.Equal(6553, samples[0]);
        Assert.Equal(6553, samples[49]);
        Assert.Equal(-6553, samples[50]);
        Assert.Equal(-6553, samples[99]);
    }

    [Fact]
    public void TriangleRisesOverTheFirstHalfAndFallsOverTheSecond()
    {
        var synth = new Synth();
        synth.NoteOn(2, Period64Hz, Steady());

        short[] samples = Render(synth, 129);

        Assert.Equal(-8191, samples[0]);
        Assert.Equal(-4095, samples[8]);
        Assert.Equal(0, samples[16]);
        Assert.Equal(8191, samples[32]);
        Assert.Equal(0, samples[48]);
        Assert.Equal(-8191, samples[64]);
        Assert.Equal(8191, samples[96]);
        Assert.Equal(-8191, samples[128]);
        for (int k = 0; k < samples.Length; k++)
        {
            double phase = k % 64 / 64.0;
            double value = phase < 0.5 ? -1 + 4 * phase : 3 - 4 * phase;
            Assert.Equal(Mix(value), samples[k]);
        }
    }

    [Fact]
    public void NoiseFollowsTheLfsrFromSeedOne()
    {
        var synth = new Synth();
        synth.NoteOn(3, NoiseStepPerSampleHz, Steady(volume: 0.5));

        short[] samples = Render(synth, 500);

        int lfsr = 1;
        for (int k = 0; k < samples.Length; k++)
        {
            double value = (lfsr & 1) != 0 ? 1 : -1;
            Assert.Equal(Mix(value * 0.5), samples[k]);
            lfsr = StepLfsr(lfsr);
        }
        Assert.Contains(Mix(0.5), samples);
        Assert.Contains(Mix(-0.5), samples);
    }

    [Fact]
    public void NoiseStepsSixteenTimesTheFrequencyPerSecond()
    {
        // 16 · f / SampleRate = 1/4: the LFSR steps once every four samples.
        double frequency = AudioFormat.SampleRate / 64.0;
        var synth = new Synth();
        synth.NoteOn(3, frequency, Steady());

        short[] samples = Render(synth, 2000);

        int lfsr = 1;
        for (int k = 0; k < samples.Length; k++)
        {
            if (k > 0 && k % 4 == 0)
            {
                lfsr = StepLfsr(lfsr);
            }
            double value = (lfsr & 1) != 0 ? 1 : -1;
            Assert.Equal(Mix(value), samples[k]);
        }
    }

    [Fact]
    public void EnvelopeDecaysLinearlyToZeroAndStaysThere()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, new Instrument("decay", 0.5, 1.0, DecayIn64Samples));

        short[] samples = Render(synth, 10_000);

        for (int k = 0; k <= 64; k++)
        {
            double value = k % 100 < 50 ? 1 : -1;
            Assert.Equal(Mix(value * (1 - k / 64.0)), samples[k]);
        }
        Assert.All(samples[64..], s => Assert.Equal(0, s));
        Assert.All(Render(synth, 1000), s => Assert.Equal(0, s));
    }

    [Fact]
    public void ChannelsAreSummedBeforeScaling()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady(0.5, 0.5));
        synth.NoteOn(1, Period100Hz, Steady(0.25, 0.3));
        synth.NoteOn(2, Period64Hz, Steady(volume: 0.6));
        synth.NoteOn(3, NoiseStepPerSampleHz, Steady(volume: 0.2));

        short[] samples = Render(synth, 300);

        int lfsr = 1;
        for (int k = 0; k < samples.Length; k++)
        {
            double pulse0 = k % 100 < 50 ? 1 : -1;
            double pulse1 = k % 100 < 25 ? 1 : -1;
            double phase = k % 64 / 64.0;
            double triangle = phase < 0.5 ? -1 + 4 * phase : 3 - 4 * phase;
            double noise = (lfsr & 1) != 0 ? 1 : -1;
            double sum = 0;
            sum += pulse0 * 0.5;
            sum += pulse1 * 0.3;
            sum += triangle * 0.6;
            sum += noise * 0.2;
            Assert.Equal(Mix(sum), samples[k]);
            lfsr = StepLfsr(lfsr);
        }
    }

    [Fact]
    public void MixIsClampedToFullScale()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady(0.5, 3.0));
        synth.NoteOn(1, Period100Hz, Steady(0.5, 3.0));

        short[] samples = Render(synth, 100);

        // 0.25 · (3 + 3) = 1.5 is clamped to 1.
        Assert.All(samples[..50], s => Assert.Equal(32767, s));
        Assert.All(samples[50..], s => Assert.Equal(-32767, s));
    }

    [Fact]
    public void SetFrequencyKeepsPhaseAndEnvelope()
    {
        // 1/256 of the envelope per sample.
        double decay = AudioFormat.SampleRate / 256.0;
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, new Instrument("decay", 0.5, 1.0, decay));

        short[] before = Render(synth, 25);
        synth.SetFrequency(0, 2 * Period100Hz);
        short[] after = Render(synth, 75);

        Assert.All(before, (s, k) => Assert.Equal(Mix(1 - k / 256.0), s));
        for (int i = 0; i < after.Length; i++)
        {
            int k = 25 + i;
            // Phase 0.25 at the change, then 1/50 per sample; never exactly on 0.5 or 1.
            double phase = (0.25 + 0.02 * i) % 1;
            double value = phase < 0.5 ? 1 : -1;
            Assert.Equal(Mix(value * (1 - k / 256.0)), after[i]);
        }
    }

    [Fact]
    public void SetFrequencyOnSilentChannelKeepsItSilent()
    {
        var synth = new Synth();
        synth.SetFrequency(1, Period100Hz);

        Assert.All(Render(synth, 500), s => Assert.Equal(0, s));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void NoteOffSilencesUntilNextNoteOn(int channel)
    {
        var synth = new Synth();
        synth.NoteOn(channel, Period100Hz, Steady());
        Assert.Contains(Render(synth, 100), s => s != 0);

        synth.NoteOff(channel);
        Assert.All(Render(synth, 1000), s => Assert.Equal(0, s));

        synth.NoteOn(channel, Period100Hz, Steady());
        var fresh = new Synth();
        fresh.NoteOn(channel, Period100Hz, Steady());
        Assert.Equal(Render(fresh, 300), Render(synth, 300));
    }

    [Fact]
    public void NoteOffLeavesOtherChannelsPlaying()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady());
        synth.NoteOn(2, Period64Hz, Steady());
        synth.NoteOff(0);

        var triangleOnly = new Synth();
        triangleOnly.NoteOn(2, Period64Hz, Steady());

        Assert.Equal(Render(triangleOnly, 500), Render(synth, 500));
    }

    [Fact]
    public void NoteOffOnSilentChannelIsAllowed()
    {
        var synth = new Synth();
        synth.NoteOff(3);

        Assert.All(Render(synth, 100), s => Assert.Equal(0, s));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void SecondNoteOnRestartsPhaseEnvelopeAndNoise(int channel)
    {
        var synth = new Synth();
        synth.NoteOn(channel, 1234.5, new Instrument("first", 0.125, 0.9, 30));
        Render(synth, 777);

        var second = new Instrument("second", 0.25, 0.6, DecayIn64Samples / 4);
        synth.NoteOn(channel, NoiseStepPerSampleHz, second);
        var fresh = new Synth();
        fresh.NoteOn(channel, NoiseStepPerSampleHz, second);

        Assert.Equal(Render(fresh, 1000), Render(synth, 1000));
    }

    [Fact]
    public void AcceptsTheNyquistFrequency()
    {
        var synth = new Synth();
        synth.NoteOn(0, AudioFormat.SampleRate / 2.0, Steady());
        synth.SetFrequency(0, AudioFormat.SampleRate / 2.0);

        // Phase 0, then 0.5 on alternate samples.
        Assert.Equal([Mix(1), Mix(-1), Mix(1), Mix(-1)], Render(synth, 4));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void NoteOnRejectsChannelOutsideRange(int channel)
    {
        var synth = new Synth();

        Assert.Throws<ArgumentException>(() => synth.NoteOn(channel, Period100Hz, Steady()));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void SetFrequencyRejectsChannelOutsideRange(int channel)
    {
        var synth = new Synth();

        Assert.Throws<ArgumentException>(() => synth.SetFrequency(channel, Period100Hz));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void NoteOffRejectsChannelOutsideRange(int channel)
    {
        var synth = new Synth();

        Assert.Throws<ArgumentException>(() => synth.NoteOff(channel));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-440.0)]
    [InlineData(22050.001)]
    [InlineData(44100.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void NoteOnRejectsFrequencyOutsideRange(double frequency)
    {
        var synth = new Synth();

        Assert.Throws<ArgumentException>(() => synth.NoteOn(0, frequency, Steady()));
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-440.0)]
    [InlineData(22050.001)]
    [InlineData(44100.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SetFrequencyRejectsFrequencyOutsideRange(double frequency)
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady());

        Assert.Throws<ArgumentException>(() => synth.SetFrequency(0, frequency));
    }

    [Fact]
    public void RejectedCallsLeaveTheChannelUnchanged()
    {
        var synth = new Synth();
        synth.NoteOn(0, Period100Hz, Steady());
        Assert.Throws<ArgumentException>(() => synth.SetFrequency(0, 0));
        Assert.Throws<ArgumentException>(() => synth.NoteOn(0, -1, Steady(0.25)));

        var reference = new Synth();
        reference.NoteOn(0, Period100Hz, Steady());

        Assert.Equal(Render(reference, 300), Render(synth, 300));
    }
}
