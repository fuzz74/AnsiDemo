# AnsiDemo

A terminal demoscene demo for Windows Terminal. Eight scenes built from twelve classic effects (plasma, fire,
tunnel, starfield, rotozoomer, 3D cube, metaballs, sine scroller, Mandelbrot zoom, voxel landscape, raymarched
spheres and Matrix rain) are drawn in 24-bit ANSI colour with half-block (`▀`) and Braille characters. The scenes
flow into each other through crossfade, wipe, dissolve and push transitions, and a four-channel chiptune, written
as tracker text and played by a small synth, comes out of the speakers through the Windows waveOut API. The whole
demo lasts 74 bars at 125 bpm (2 minutes 22 seconds).

Every frame is deterministic for a timestamp, so single frames and the soundtrack can also be rendered without a
terminal, which is how the tests check them.

## Requirements

- Windows with [Windows Terminal](https://aka.ms/terminal), in a window of at least 80x24 (maximized looks best)
- .NET SDK 10
- A sound device for the music (optional: without one the demo plays silently)

## Run

```powershell
dotnet restore AnsiDemo.slnx
dotnet run --project src/AnsiDemo
```

The demo plays once with music and exits when it ends. Arguments go after `--`:

```powershell
dotnet run --project src/AnsiDemo -- --loop
dotnet run --project src/AnsiDemo -- --scene fire
dotnet run --project src/AnsiDemo -- --effect plasma
dotnet run --project src/AnsiDemo -- --frame 20
dotnet run --project src/AnsiDemo -- --frame 3 --effect mandelbrot --size 160x48
dotnet run --project src/AnsiDemo -- --wav $env:TEMP/ansidemo.wav
```

### Flags

| Flag | Effect |
| --- | --- |
| `--no-sound` | Play the demo without music. |
| `--loop` | Start over at the beginning, music included, when the demo ends. |
| `--scene <name>` | Loop only that scene, without transitions and without music. |
| `--effect <name>` | Loop only that effect, alone in its preferred pixel mode, without music. |
| `--frame <seconds>` | Write the single frame at that time to stdout as ANSI text (UTF-8, no BOM) and exit, without touching the console. Combine with `--scene` or `--effect` to render that alone. |
| `--size <cols>x<rows>` | The frame size for `--frame` (default `80x24`). |
| `--wav <path>` | Write the soundtrack for the length of the demo to a 16-bit mono 44100 Hz WAV file and exit. |

`--scene` and `--effect` exclude each other, `--size` only goes with `--frame`, and `--wav` stands alone. An
unknown or malformed argument, an unknown scene or effect, or a `--frame` time outside the demo (or outside the
scene or effect) prints the usage and exits with code 2:

```
Usage: AnsiDemo [--no-sound] [--loop] [--scene <name> | --effect <name>]
       AnsiDemo --frame <seconds> [--scene <name> | --effect <name>] [--size <cols>x<rows>]
       AnsiDemo --wav <path>
Keys: Q/Esc quit, Space pause, Left/Right previous/next scene
```

### Keys

| Key | Action |
| --- | --- |
| `Q` / `Esc` | Quit and give the terminal back. `Ctrl+C` quits too. |
| `Space` | Pause and resume the picture and the music. |
| `Left` / `Right` | Jump to the start of the previous / next scene; the music follows. |

While the window is smaller than 80x24 the demo shows only a message asking for a larger window and responds only
to the quit keys; time keeps running. Left and Right do nothing with `--scene` or `--effect`.

## Scenes

Each bar is 1.92 s. Every scene starts with a one-bar transition from the previous one; the first fades in from a
blank screen.

| # | Scene | Pixel mode | Effects (back to front) | Bars | Transition in |
| --- | --- | --- | --- | --- | --- |
| 0 | `boot` | Braille | metaballs, matrix | 8 | crossfade |
| 1 | `title` | half-block | plasma, scroller | 10 | dissolve |
| 2 | `tunnel` | half-block | tunnel, cube | 10 | wipe |
| 3 | `fire` | half-block | fire | 8 | crossfade |
| 4 | `roto` | half-block | rotozoomer | 8 | push |
| 5 | `flight` | half-block | starfield, voxel | 10 | dissolve |
| 6 | `mandel` | half-block | mandelbrot | 10 | crossfade |
| 7 | `spheres` | Braille | raymarch | 10 | wipe |

## Effects

The names below are what `--effect` accepts.

| Name | Preferred mode | What it shows |
| --- | --- | --- |
| `plasma` | half-block | Smooth colour blobs from a sum of sines, cycling through the hues |
| `fire` | half-block | Flames rising from the bottom edge, black to red to yellow to white |
| `tunnel` | half-block | A textured tunnel seen from inside, rotating and flying forward |
| `starfield` | Braille | Stars streaming outward from the centre, brighter when near |
| `rotozoomer` | half-block | A tiled texture rotating and zooming in and out |
| `cube` | Braille | A wireframe cube rotating about two axes in perspective |
| `metaballs` | Braille | Glowing blobs that merge and split |
| `scroller` | half-block | The greetings text scrolling right to left on a sine wave |
| `mandelbrot` | half-block | A continuous zoom into the edge of the Mandelbrot set |
| `voxel` | half-block | A Comanche-style flight over a heightmap landscape with fog |
| `raymarch` | Braille | Shaded spheres, one orbiting, over a checkered floor |
| `matrix` | Braille | Falling columns of glyphs with bright heads and fading green tails |

## Golden frames

The tests compare rendered frames with golden `.ansi` files: the raw ANSI text of the frame, which the terminal
displays directly. Each effect has goldens at 0, 1 and 4 s in `tests/AnsiDemo.Tests/Effects/<Folder>/`, and the
whole demo has two per scene in `tests/AnsiDemo.Tests/Demo/`: `scene<i>-mid.ansi` in the middle of the transition
into scene i and `scene<i>-in.ansi` just after it. View one with:

```powershell
Get-Content tests/AnsiDemo.Tests/Effects/Plasma/t4.ansi
Get-Content tests/AnsiDemo.Tests/Demo/scene3-in.ansi
```

A missing golden is recorded by the test that asks for it, which then fails once with "golden recorded, rerun".
On a mismatch the test writes `<name>.actual.ansi` beside the golden (ignored by git). Setting
`ANSIDEMO_UPDATE_GOLDENS=1` re-records every golden that is checked; view each one before committing it.

## Tests

```powershell
# whole project
dotnet build AnsiDemo.slnx -warnaserror && dotnet test AnsiDemo.slnx --no-build

# one module: Contracts, Core, Audio, Tracker, Transitions, Music, Effects.<Folder> or Demo
dotnet test AnsiDemo.slnx --filter "FullyQualifiedName~AnsiDemo.Tests.Demo"

# without the frame-time budgets or the sound device tests
dotnet test AnsiDemo.slnx --filter "Category!=Perf&Category!=Device"
```

Device tests open the sound card and return early when there is none. No test plays the demo in the console.
