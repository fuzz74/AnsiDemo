using AnsiDemo.Contracts;

namespace AnsiDemo.Audio;

/// <summary>
/// Plays a sample source as 16-bit mono PCM through the default wave-out device (WAVE_MAPPER). A background feeder
/// thread keeps <see cref="BufferCount"/> buffers of <see cref="SamplesPerBuffer"/> samples queued, refilling each one
/// as the driver marks it done. A sink plays once: after <see cref="Stop"/> it cannot be started again.
/// </summary>
public sealed class WaveOutSink : IAudioSink
{
    private const int BufferCount = 4;
    private const int SamplesPerBuffer = 2048;
    private const int PollMilliseconds = 5;
    private static readonly TimeSpan FeederJoinTimeout = TimeSpan.FromMilliseconds(250);

    private readonly Lock _lifecycle = new();   // serialises Start, Stop and Dispose
    private readonly Lock _device = new();      // guards _handle and the buffers between Stop and the feeder
    private readonly List<WaveOutBuffer> _buffers = new(BufferCount);
    private IntPtr _handle;
    private Thread? _feeder;
    private bool _started;
    private bool _stopping;
    private bool _disposed;

    /// <summary>Number of wave-out devices (waveOutGetNumDevs).</summary>
    public static int DeviceCount => (int)WaveOutNative.waveOutGetNumDevs();

    public void Start(ISampleSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        lock (_lifecycle)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_started)
            {
                throw new InvalidOperationException("The wave-out sink has already been started.");
            }
            if (DeviceCount == 0)
            {
                throw new AudioDeviceException("No wave-out device is available.", WaveOutNative.MmsyserrBadDeviceId);
            }

            var format = WaveOutFormat.Pcm16Mono(AudioFormat.SampleRate);
            int result = WaveOutNative.waveOutOpen(out var handle, WaveOutNative.WaveMapper, ref format,
                                                   IntPtr.Zero, IntPtr.Zero, WaveOutNative.CallbackNull);
            ThrowOnError(result, "waveOutOpen");
            _handle = handle;

            try
            {
                for (int i = 0; i < BufferCount; i++)
                {
                    var buffer = new WaveOutBuffer(SamplesPerBuffer);
                    _buffers.Add(buffer);
                    ThrowOnError(buffer.Prepare(_handle), "waveOutPrepareHeader");
                }
                // Queue every buffer before returning so playback starts at once.
                foreach (var buffer in _buffers)
                {
                    source.Render(buffer.Samples);
                    ThrowOnError(buffer.Write(_handle), "waveOutWrite");
                }
            }
            catch
            {
                ReleaseDevice();
                throw;
            }

            var buffers = _buffers.ToArray();
            _feeder = new Thread(() => Feed(source, buffers))
            {
                IsBackground = true,
                Name = "AnsiDemo wave-out feeder",
                Priority = ThreadPriority.AboveNormal,
            };
            _started = true;
            _feeder.Start();
        }
    }

    public void Stop()
    {
        lock (_lifecycle)
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }
            lock (_device)
            {
                _stopping = true;
            }
            // The feeder touches the device only under _device after checking _stopping, so the device can be
            // released even if a slow source keeps the feeder from ending within the timeout.
            _feeder?.Join(FeederJoinTimeout);
            ReleaseDevice();
        }
    }

    public void Dispose()
    {
        lock (_lifecycle)
        {
            _disposed = true;
        }
        Stop();
    }

    private void Feed(ISampleSource source, WaveOutBuffer[] buffers)
    {
        while (true)
        {
            Thread.Sleep(PollMilliseconds);
            foreach (var buffer in buffers)
            {
                lock (_device)
                {
                    if (_stopping)
                    {
                        return;
                    }
                    if (!buffer.IsDone)
                    {
                        continue;
                    }
                }

                try
                {
                    source.Render(buffer.Samples);
                }
                catch (Exception)
                {
                    // An exception escaping a background thread would terminate the process: a failing source
                    // ends playback instead, and Stop still releases the device.
                    return;
                }

                lock (_device)
                {
                    if (_stopping)
                    {
                        return;
                    }
                    if (buffer.Write(_handle) != WaveOutNative.MmsyserrNoError)
                    {
                        // The device is gone (e.g. unplugged): playback ends; Stop still releases the handle.
                        return;
                    }
                }
            }
        }
    }

    private void ReleaseDevice()
    {
        WaveOutNative.waveOutReset(_handle);
        foreach (var buffer in _buffers)
        {
            buffer.Release(_handle);
        }
        _buffers.Clear();
        WaveOutNative.waveOutClose(_handle);
        _handle = IntPtr.Zero;
        _feeder = null;
    }

    private static void ThrowOnError(int result, string function)
    {
        if (result != WaveOutNative.MmsyserrNoError)
        {
            throw new AudioDeviceException($"{function} failed with MMRESULT {result}.", result);
        }
    }
}
