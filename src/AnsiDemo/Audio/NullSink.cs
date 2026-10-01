using AnsiDemo.Contracts;

namespace AnsiDemo.Audio;

/// <summary>A sink that plays nothing: it accepts <see cref="Start"/> and <see cref="Stop"/> and never renders the source.</summary>
public sealed class NullSink : IAudioSink
{
    public void Start(ISampleSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
