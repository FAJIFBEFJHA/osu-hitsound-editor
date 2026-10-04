# PROJECT_CONTEXT

Last updated: 2026-10-04

## Project

`osu-hitsound-editor`

C#/.NET learning project focused on building an osu!standard hitsound editor with a DAW-like workflow.

Repository:

```text
https://github.com/FAJIFBEFJHA/osu-hitsound-editor
```

The repository is public.

## Current stage

**Physical Sample Resolution**

Current test status:

```text
168/168 passing
```

Hitsound Resolution was completed before starting Physical Sample Resolution.

## Current objective

Preserve or recreate a reproducible explicit-filename compatibility fixture and record each input case before changing logical hitsound generation.

The compatibility policy is now decided: legacy `.osu` explicit custom filenames use documented/osu!stable custom-only semantics as the canonical logical behavior. Do not modify `Beatmap.GetHitSoundLayers(...)` until the fixture is recorded and ready to support regression tests.

## Current implementation checkpoint

### Logical hitsound resolution

The logical layer is already implemented and covered by the previous Hitsound Resolution checkpoint.

Relevant behavior includes:

- effective `SampleSet`
- effective addition sample set
- effective `SampleIndex`
- effective `Volume`
- standard hitobject hitsounds
- slider edge hitsounds
- slider body `SliderSlide`
- slider body `SliderWhistle`
- slider tick timing
- slider tick hitsounds
- custom filename representation

Relevant `Beatmap.cs` organization:

```text
PROPERTIES

RESOLUTION HELPERS

TIMING
    GetActiveTimingPoint
    GetActiveUninheritedTimingPoint
    GetActiveInheritedTimingPoint
    GetEffectiveSliderVelocityMultiplier

SLIDER TIMING
    GetSliderSpanDuration
    GetSliderDuration
    GetSliderEdgeTime
    GetSliderTickDistance
    GetSliderTickTimes

SAMPLE SETS

HITOBJECT - EFFECTIVE VALUES

SLIDER EDGE - EFFECTIVE VALUES

HITSOUND TYPES

HITSOUND LAYERS
    GetHitSoundLayers(HitObject)
    GetHitSoundLayers(SliderEdge, HitSample, double time)
    GetSliderEdgeHitSoundLayers
    GetSliderBodyHitSoundLayers
    GetSliderTickHitSoundLayers
```

Keep slider edge, body, and tick responsibilities separate for now. There is no `GetAllSliderHitSounds()` API.

### Physical sample resolution boundary

`SampleResolver` now owns physical osu! sample lookup behavior.

Implemented methods:

```text
GetStandardSampleLookup(HitSoundLayer layer)
GetSliderBodySampleLookup(HitSoundLayer layer)
GetSliderTickSampleLookup(HitSoundLayer layer)
ResolveCustomSamplePath(HitSoundLayer layer, string beatmapDirectory)
ResolveBeatmapSamplePath(string? beatmapFilename, string beatmapDirectory)
ResolveSample(HitSoundLayer layer, string beatmapDirectory)
```

The existing lookup methods intentionally return:

```text
BeatmapFilename
FallbackFilename
```

rather than collapsing the lookup into one filename.

### Standard sample name mappings

Current mappings:

```text
Normal          -> hitnormal
Whistle         -> hitwhistle
Finish          -> hitfinish
Clap            -> hitclap
SliderSlide     -> sliderslide
SliderWhistle   -> sliderwhistle
SliderTick      -> slidertick
```

### SampleIndex semantics

Physical resolution preserves the distinction between `SampleIndex = 0` and `SampleIndex = 1`.

Current rule:

```text
SampleIndex = 0
    BeatmapFilename = null
    fallback remains available

SampleIndex = 1
    BeatmapFilename = unindexed standard filename
    fallback = same unindexed standard filename

SampleIndex > 1
    BeatmapFilename = indexed standard filename
    fallback = unindexed standard filename
```

Therefore, a matching unindexed file in the beatmap directory must not be used when the logical layer has `SampleIndex = 0`.

### Beatmap sample extension lookup

Verified against the official `ppy/osu` implementation.

Legacy sample lookup order is:

```text
.wav
.mp3
.ogg
```

`ResolveBeatmapSamplePath(...)` checks those extensions in that order and returns the first physical path found.

### Explicit custom filename lookup

`ResolveCustomSamplePath(...)` treats an explicit filename separately from standard sample lookup.

Current result:

```text
file exists
    -> full physical path

file does not exist
    -> null
```

A missing explicit custom filename is not treated as ordinary external fallback.

### Physical resolution result model

Implemented:

```text
SampleResolutionOutcome
SampleResolutionResult
```

`SampleResolutionOutcome` values:

```text
BeatmapSampleFound
ExternalFallbackRequired
CustomSampleFound
CustomSampleMissing
```

`SampleResolutionResult` carries:

```text
Outcome
ResolvedPath
FallbackFilename
```

Interpretation:

```text
BeatmapSampleFound
    ResolvedPath = physical beatmap sample path
    FallbackFilename = standard fallback filename

ExternalFallbackRequired
    ResolvedPath = null
    FallbackFilename = standard fallback filename

CustomSampleFound
    ResolvedPath = explicit custom file path
    FallbackFilename = null

CustomSampleMissing
    ResolvedPath = null
    FallbackFilename = null
```

### Integrated resolution

`ResolveSample(...)` coordinates the previous methods.

Flow:

```text
HitSoundLayer
    -> Custom
        -> ResolveCustomSamplePath
        -> CustomSampleFound / CustomSampleMissing

    -> Normal / Whistle / Finish / Clap
        -> GetStandardSampleLookup

    -> SliderSlide / SliderWhistle
        -> GetSliderBodySampleLookup

    -> SliderTick
        -> GetSliderTickSampleLookup

    standard/slider lookup
        -> ResolveBeatmapSamplePath
        -> BeatmapSampleFound / ExternalFallbackRequired
```

## Test progression for Physical Sample Resolution

Known checkpoints from the current development session:

```text
100/100  Hitsound Resolution baseline before physical lookup work
114/114  standard sample lookup
126/126  slider body sample lookup
138/138  slider tick sample lookup
148/148  explicit custom sample path lookup
159/159  beatmap sample extension lookup
168/168  integrated physical sample resolution
```

Treat the actual test suite as the implementation truth if this list ever becomes stale.

## osu!stable vs osu!lazer explicit filename discrepancy

A compatibility difference was confirmed manually using both installed clients.

### Official implementation evidence

In the current `ppy/osu` legacy parser, an explicit filename creates a `FileHitSampleInfo`, while `Finish`, `Whistle`, and `Clap` flags are still converted into additional sample entries.

This means the lazer-side logical representation can contain:

```text
explicit custom file
+ addition samples selected by hitSound flags
```

### Manual osu!stable result

For the seven-object manual comparison beatmap, the audible osu!stable results in order were:

```text
normal
custom
custom
custom
custom
custom
normal + clap
```

The tested explicit-filename cases therefore behaved as custom-only in stable.

### Manual osu!lazer result

Running the same comparison in osu!lazer produced the combined behavior expected from lazer's current handling: the explicit custom sample and applicable addition samples can coexist.

### Important limitation

The exact seven input combinations are not recorded in this checkpoint. Do not reconstruct the full case matrix from the output sequence alone.

Before changing production semantics, retain or recreate the test beatmap and record each input case explicitly.

### Current decision

The project uses documented legacy/osu!stable semantics as the canonical logical behavior for an explicit `hitSample.filename`.

Canonical interpretation:

```text
explicit hitSample.filename
    -> custom sample only
```

The original parsed `hitSound` flags and `hitSample.filename` remain preserved as source data. Suppression of `Clap`, `Whistle`, and `Finish` additions belongs to logical hitsound resolution rather than parsing.

The current osu!lazer behavior, where the custom sample can coexist with applicable additions, is treated as client-specific behavior. Do not introduce a stable/lazer compatibility mode unless a concrete future consumer requires it.

Before changing `GetHitSoundLayers(...)`, preserve or recreate the explicit-filename comparison fixture and record every input case explicitly.

## Tooling checkpoint

A repository `.editorconfig` is present locally to align VS Code/C# naming diagnostics and suggestions with the project's established naming conventions.

## Refactoring status

There is visible duplication among standard, slider-body, and slider-tick filename lookup methods.

Do not refactor it yet.

The compatibility policy is now decided, but the explicit-filename fixture, logical behavior change, and regression tests are not complete. A helper abstraction remains premature until that behavior is implemented and verified.

## Deferred work

Do not mix these into the current compatibility investigation unless they become blockers.

- Define stable logical `SampleId` only after physical-resolution semantics are trustworthy.
- Audio decoding/playback belongs to the later Audio Infrastructure stage.
- Export and round-trip equivalence come before global optimization.
- Do not introduce CP-SAT or another global optimizer yet.
- Use `CultureInfo.InvariantCulture` for `.osu` numeric parsing when parser robustness work resumes.
- Improve malformed/empty parser field handling later.

## Exact next task

Create or preserve a reproducible explicit-filename compatibility fixture and record each input case explicitly before modifying `GetHitSoundLayers(...)` to implement the chosen custom-only legacy semantics.

## Source-of-truth order

When sources disagree, use this order:

1. Tests and actual source code
2. Official osu! documentation / official open-source implementation for format behavior
3. `docs/DECISIONS.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/ROADMAP.md`
6. Chat history or AI suggestions
