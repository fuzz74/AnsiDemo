using AnsiDemo.Shell;

namespace AnsiDemo.Tests.Demo;

public class ConsoleKeyMapTests
{
    [Theory]
    [InlineData(ConsoleKey.Q, 'q', false, ConsoleCommand.Quit)]
    [InlineData(ConsoleKey.Q, 'Q', false, ConsoleCommand.Quit)]
    [InlineData(ConsoleKey.Escape, '\u001b', false, ConsoleCommand.Quit)]
    [InlineData(ConsoleKey.C, '\u0003', true, ConsoleCommand.Quit)]
    [InlineData((ConsoleKey)0, '\u0003', false, ConsoleCommand.Quit)]
    [InlineData(ConsoleKey.Spacebar, ' ', false, ConsoleCommand.Pause)]
    [InlineData(ConsoleKey.RightArrow, '\0', false, ConsoleCommand.NextScene)]
    [InlineData(ConsoleKey.LeftArrow, '\0', false, ConsoleCommand.PreviousScene)]
    [InlineData(ConsoleKey.C, 'c', false, ConsoleCommand.None)]
    [InlineData(ConsoleKey.UpArrow, '\0', false, ConsoleCommand.None)]
    [InlineData(ConsoleKey.Enter, '\r', false, ConsoleCommand.None)]
    public void KeysMapToTheirCommands(ConsoleKey key, char keyChar, bool control, ConsoleCommand expected)
    {
        var info = new ConsoleKeyInfo(keyChar, key, shift: false, alt: false, control);

        Assert.Equal(expected, ConsoleKeyMap.CommandFor(info));
    }
}
