using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AnsiDemo.Tests.Contracts;

public class BannedApiTests
{
    // N.1: no console, unseeded randomness or wall clock in the contracts.
    private static readonly Regex Banned = new(@"\b(Console|Random|DateTime\w*|Stopwatch)\b", RegexOptions.CultureInvariant);

    [Fact]
    public void ContractsAvoidConsoleRandomAndClock()
    {
        string contracts = Path.Combine(FindRepoRoot(), "src", "AnsiDemo", "Contracts");
        string[] files = Directory.GetFiles(contracts, "*.cs");
        Assert.NotEmpty(files);

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
