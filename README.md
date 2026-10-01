# AnsiDemo

A terminal demoscene demo for Windows Terminal: eight scenes built from twelve classic effects, drawn in
24-bit ANSI colour with half-block and Braille characters, with a chiptune soundtrack played through waveOut.

## Requirements

- Windows with Windows Terminal
- .NET SDK 10

## Setup

```powershell
dotnet restore AnsiDemo.slnx
```

## Test

```powershell
dotnet build AnsiDemo.slnx -warnaserror && dotnet test AnsiDemo.slnx --no-build
```

## Run

```powershell
dotnet run --project src/AnsiDemo
```
