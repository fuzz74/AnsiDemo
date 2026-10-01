using AnsiDemo.Contracts;

namespace AnsiDemo.Shell;

/// <summary>The demo's scenes in playing order (the scene table of the spec); the bars total <see cref="DemoClock.DemoBars"/>.</summary>
public static class DemoScript
{
    public static IReadOnlyList<SceneSpec> Scenes { get; } =
    [
        new("boot", PixelMode.Braille, ["metaballs", "matrix"], 8, "crossfade", 1),
        new("title", PixelMode.HalfBlock, ["plasma", "scroller"], 10, "dissolve", 1),
        new("tunnel", PixelMode.HalfBlock, ["tunnel", "cube"], 10, "wipe", 1),
        new("fire", PixelMode.HalfBlock, ["fire"], 8, "crossfade", 1),
        new("roto", PixelMode.HalfBlock, ["rotozoomer"], 8, "push", 1),
        new("flight", PixelMode.HalfBlock, ["starfield", "voxel"], 10, "dissolve", 1),
        new("mandel", PixelMode.HalfBlock, ["mandelbrot"], 10, "crossfade", 1),
        new("spheres", PixelMode.Braille, ["raymarch"], 10, "wipe", 1),
    ];
}
