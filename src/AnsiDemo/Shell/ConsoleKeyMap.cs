namespace AnsiDemo.Shell;

/// <summary>The demo's keys: Q, Escape and Ctrl+C quit, Space pauses, Right and Left skip scenes.</summary>
public static class ConsoleKeyMap
{
    /// <summary>
    /// The command of <paramref name="key"/>. Ctrl+C arrives as C with the Control modifier because the host sets
    /// <see cref="Console.TreatControlCAsInput"/> (some consoles report only its character, U+0003).
    /// </summary>
    public static ConsoleCommand CommandFor(ConsoleKeyInfo key) => key.Key switch
    {
        ConsoleKey.Q or ConsoleKey.Escape => ConsoleCommand.Quit,
        ConsoleKey.C when (key.Modifiers & ConsoleModifiers.Control) != 0 => ConsoleCommand.Quit,
        _ when key.KeyChar == '\u0003' => ConsoleCommand.Quit,
        ConsoleKey.Spacebar => ConsoleCommand.Pause,
        ConsoleKey.RightArrow => ConsoleCommand.NextScene,
        ConsoleKey.LeftArrow => ConsoleCommand.PreviousScene,
        _ => ConsoleCommand.None,
    };
}
