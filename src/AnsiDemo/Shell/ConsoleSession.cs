using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace AnsiDemo.Shell;

/// <summary>
/// Puts the terminal into demo mode: virtual terminal processing on stdout, UTF-8 output, the alternate screen buffer
/// and a hidden cursor. <see cref="Dispose"/> restores all four, once, however the demo ends.
/// </summary>
internal sealed class ConsoleSession : IDisposable
{
    private const string Enter = "\e[?1049h\e[?25l";
    private const string Leave = "\e[0m\e[?25h\e[?1049l";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Stream _stdout;
    private readonly IntPtr _handle;
    private uint _previousMode;
    private bool _modeChanged;
    private Encoding? _previousEncoding;
    private bool _screenEntered;
    private bool _disposed;
    private char[] _chars = [];
    private byte[] _bytes = [];

    public ConsoleSession()
    {
        _stdout = Console.OpenStandardOutput();
        _handle = ConsoleNative.GetStdHandle(ConsoleNative.StdOutputHandle);
        try
        {
            if (_handle == IntPtr.Zero || _handle == ConsoleNative.InvalidHandleValue
                || !ConsoleNative.GetConsoleMode(_handle, out _previousMode))
            {
                throw new IOException("The output is not a console; use --frame to render without one.");
            }
            if (!ConsoleNative.SetConsoleMode(_handle, _previousMode | ConsoleNative.EnableVirtualTerminalProcessing))
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "The console does not support virtual terminal sequences.");
            }
            _modeChanged = true;

            _previousEncoding = Console.OutputEncoding;
            Console.OutputEncoding = Utf8;

            _screenEntered = true;
            WriteRaw(Enter);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Writes <paramref name="text"/> to stdout as UTF-8 in a single write.</summary>
    public void Write(StringBuilder text)
    {
        ArgumentNullException.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(_disposed, this);
        int length = text.Length;
        if (_chars.Length < length)
        {
            _chars = new char[length];
        }
        text.CopyTo(0, _chars, 0, length);
        int maxBytes = Utf8.GetMaxByteCount(length);
        if (_bytes.Length < maxBytes)
        {
            _bytes = new byte[maxBytes];
        }
        int count = Utf8.GetBytes(_chars, 0, length, _bytes, 0);
        _stdout.Write(_bytes, 0, count);
        _stdout.Flush();
    }

    /// <summary>Resets the colours, shows the cursor, leaves the alternate screen and restores encoding and mode.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        try
        {
            if (_screenEntered)
            {
                WriteRaw(Leave);
            }
        }
        finally
        {
            try
            {
                if (_previousEncoding is not null)
                {
                    Console.OutputEncoding = _previousEncoding;
                }
            }
            finally
            {
                if (_modeChanged)
                {
                    ConsoleNative.SetConsoleMode(_handle, _previousMode);
                }
            }
        }
    }

    private void WriteRaw(string text)
    {
        byte[] bytes = Utf8.GetBytes(text);
        _stdout.Write(bytes, 0, bytes.Length);
        _stdout.Flush();
    }
}
