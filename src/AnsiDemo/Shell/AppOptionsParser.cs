using System.Globalization;
using AnsiDemo.Core;

namespace AnsiDemo.Shell;

/// <summary>Parses the command line into <see cref="AppOptions"/>.</summary>
public static class AppOptionsParser
{
    public const string Usage =
        "Usage: AnsiDemo [--no-sound] [--loop] [--scene <name> | --effect <name>]\n" +
        "       AnsiDemo --frame <seconds> [--scene <name> | --effect <name>] [--size <cols>x<rows>]\n" +
        "       AnsiDemo --wav <path>\n" +
        "Keys: Q/Esc quit, Space pause, Left/Right previous/next scene";

    public const int DefaultCols = 80, DefaultRows = 24;

    /// <summary>
    /// Accepts exactly the three forms of <see cref="Usage"/>; any other input throws an
    /// <see cref="ArgumentException"/> whose message is <see cref="Usage"/>. The upper bound of
    /// <c>--frame</c> depends on what is rendered and is left to the host.
    /// </summary>
    public static AppOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool noSound = false, loop = false, sized = false;
        string? scene = null, effect = null, wav = null;
        double? frame = null;
        int cols = DefaultCols, rows = DefaultRows;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < args.Length; i++)
        {
            string flag = args[i];
            if (!seen.Add(flag))
            {
                throw UsageError();
            }

            switch (flag)
            {
                case "--no-sound":
                    noSound = true;
                    break;
                case "--loop":
                    loop = true;
                    break;
                case "--scene":
                    scene = ParseScene(ValueOf(args, ref i));
                    break;
                case "--effect":
                    effect = ParseEffect(ValueOf(args, ref i));
                    break;
                case "--frame":
                    frame = ParseSeconds(ValueOf(args, ref i));
                    break;
                case "--size":
                    (cols, rows) = ParseSize(ValueOf(args, ref i));
                    sized = true;
                    break;
                case "--wav":
                    wav = ValueOf(args, ref i);
                    break;
                default:
                    throw UsageError();
            }
        }

        bool wavWithOthers = wav is not null && seen.Count > 1;
        bool sceneAndEffect = scene is not null && effect is not null;
        bool sizeWithoutFrame = sized && frame is null;
        bool playFlagsWithFrame = frame is not null && (noSound || loop);
        if (wavWithOthers || sceneAndEffect || sizeWithoutFrame || playFlagsWithFrame)
        {
            throw UsageError();
        }
        return new AppOptions(noSound, loop, scene, effect, frame, cols, rows, wav);
    }

    private static string ValueOf(string[] args, ref int i)
    {
        if (i + 1 >= args.Length || string.IsNullOrEmpty(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw UsageError();
        }
        return args[++i];
    }

    private static string ParseScene(string name) =>
        DemoScript.Scenes.Any(scene => string.Equals(scene.Name, name, StringComparison.Ordinal)) ? name : throw UsageError();

    private static string ParseEffect(string name) =>
        EffectCatalog.Names.Contains(name, StringComparer.Ordinal) ? name : throw UsageError();

    private static double ParseSeconds(string text)
    {
        const NumberStyles Styles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent;
        return double.TryParse(text, Styles, CultureInfo.InvariantCulture, out double seconds) && double.IsFinite(seconds) && seconds >= 0
            ? seconds
            : throw UsageError();
    }

    private static (int Cols, int Rows) ParseSize(string text)
    {
        string[] parts = text.Split('x');
        return parts.Length == 2 && TryParsePositive(parts[0], out int cols) && TryParsePositive(parts[1], out int rows)
            ? (cols, rows)
            : throw UsageError();
    }

    private static bool TryParsePositive(string text, out int value) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;

    private static ArgumentException UsageError() => new(Usage);
}
