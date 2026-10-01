using AnsiDemo.Contracts;

namespace AnsiDemo.Tests.Core;

/// <summary>One call of <see cref="RecordingTransition.Apply"/>, with its input frames as ANSI text.</summary>
public sealed record TransitionCall(ITransition Transition, string From, string To, double Progress);
