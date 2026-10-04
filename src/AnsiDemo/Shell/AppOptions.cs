namespace AnsiDemo.Shell;

/// <summary>The parsed command line; see <see cref="AppOptionsParser"/>.</summary>
/// <param name="Frame">Timestamp in seconds of the single frame to render headless, or null to play.</param>
/// <param name="Cols">Columns of the headless frame.</param>
/// <param name="Rows">Rows of the headless frame.</param>
/// <param name="Wav">Path of the WAV file to write the soundtrack to, or null.</param>
public sealed record AppOptions(bool NoSound, bool Loop, string? Scene, string? Effect, double? Frame, int Cols, int Rows, string? Wav);
