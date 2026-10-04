using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>Runner test effect: logs the cell it finds at (0, 0), then writes <see cref="Ch"/> there in white on black.</summary>
[Effect("test-rec-cell", PixelMode.HalfBlock)]
public sealed class RecordingCellEffect : RecordingEffect, ICellEffect
{
    public char Ch => (char)('A' + Id);

    public void Render(Frame frame)
    {
        Cell cell = frame[0, 0];
        TestLog.Renders.Add($"{Id} cell saw {cell.Ch} {Format(cell.Fg)}");
        frame[0, 0] = new Cell(Ch, Rgb.White, Rgb.Black);
    }
}
