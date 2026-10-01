namespace AnsiDemo.Contracts;

public static class DemoClock
{
    public const int Bpm = 125, BeatsPerBar = 4, DemoBars = 74, HeadlessFps = 30;
    public const double SecondsPerBar = 60.0 * BeatsPerBar / Bpm;
    public const double HeadlessStep = 1.0 / HeadlessFps;
}
