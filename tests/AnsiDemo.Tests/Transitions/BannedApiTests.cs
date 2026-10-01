using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AnsiDemo.Contracts;
using AnsiDemo.Transitions;

namespace AnsiDemo.Tests.Transitions;

public class BannedApiTests
{
    // N.1: no console, unseeded randomness or wall clock in the transitions.
    private static readonly Regex Banned = new(@"\b(Console|Random|DateTime\w*|Stopwatch)\b", RegexOptions.CultureInvariant);

    [Fact]
    public void TransitionsAvoidConsoleRandomAndClock()
    {
        string transitions = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Transitions");
        string[] files = Directory.GetFiles(transitions, "*.cs");
        Assert.Contains(files, file => Path.GetFileName(file) == nameof(CrossfadeTransition) + ".cs");

        var hits = new List<string>();
        foreach (string file in files)
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (Banned.IsMatch(lines[i]))
                {
                    hits.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }
        Assert.Empty(hits);
    }

    [Fact]
    public void PatternCatchesTheBannedIdentifiersButNotDemoRandom()
    {
        Assert.Matches(Banned, "System.Console.Write(x);");
        Assert.Matches(Banned, "var r = new Random();");
        Assert.Matches(Banned, "DateTimeOffset.Now");
        Assert.Matches(Banned, "Stopwatch.StartNew()");
        Assert.DoesNotMatch(Banned, $"{nameof(DemoRandom)}.Hash(x, y)");
    }

    private static string FindRepoRoot([CallerFilePath] string callerFile = "")
    {
        for (string? dir = Path.GetDirectoryName(callerFile); dir is not null; dir = Path.GetDirectoryName(dir))
        {
            if (File.Exists(Path.Combine(dir, "AnsiDemo.slnx")))
            {
                return dir;
            }
        }
        throw new InvalidOperationException($"AnsiDemo.slnx not found above {callerFile}.");
    }
}
