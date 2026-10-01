using System.Runtime.CompilerServices;
using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class ReadmeTests
{
    [Theory]
    [InlineData("--no-sound")]
    [InlineData("--loop")]
    [InlineData("--scene <name>")]
    [InlineData("--effect <name>")]
    [InlineData("--frame <seconds>")]
    [InlineData("--size <cols>x<rows>")]
    [InlineData("--wav <path>")]
    public void ReadmeListsTheFlag(string flag)
    {
        Assert.Contains($"`{flag}`", Readme());
    }

    [Theory]
    [InlineData("Q")]
    [InlineData("Esc")]
    [InlineData("Ctrl+C")]
    [InlineData("Space")]
    [InlineData("Left")]
    [InlineData("Right")]
    public void ReadmeListsTheKey(string key)
    {
        Assert.Contains($"`{key}`", Readme());
    }

    [Fact]
    public void ReadmeListsEveryScene()
    {
        string readme = Readme();
        Assert.All(DemoScript.Scenes, scene => Assert.Contains($"`{scene.Name}`", readme));
    }

    [Fact]
    public void ReadmeShowsTheUsageText()
    {
        Assert.Contains(AppOptionsParser.Usage, Readme());
    }

    private static string Readme() => File.ReadAllText(Path.Combine(FindRepoRoot(), "README.md")).Replace("\r", "");

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
