using System;

namespace OsuHitsoundEditor;

public class SampleResolutionResult
{
    public SampleResolutionOutcome Outcome { get; set; }
    public string? ResolvedPath { get; set; }
    public string? FallbackFilename { get; set; }
}
