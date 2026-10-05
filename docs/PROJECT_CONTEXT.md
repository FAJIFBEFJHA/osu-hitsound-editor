# PROJECT_CONTEXT

Last updated: 2026-10-05

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
182/182 passing
```

Hitsound Resolution was completed before starting Physical Sample Resolution.

## Current objective

Close the explicit-filename compatibility work with reproducible fixtures and regression coverage, then verify the remaining unresolved legacy slider inheritance case before continuing Physical Sample Resolution.

The canonical compatibility policy is implemented: legacy `.osu` explicit custom filenames resolve to the custom sample only. Slider trailing `hitSample.index`, `hitSample.volume`, and `hitSample.filename` are preserved as source data but do not override legacy slider edge/body/tick sample resolution.

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
    GetSampleTimingPoint

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
173/173  explicit-filename hitobject compatibility
176/176  remaining spinner/slider compatibility regression setup
182/182  legacy slider sample-source semantics and documented fallback coverage
```

Treat the actual test suite as the implementation truth if this list ever becomes stale.

## osu!stable vs osu!lazer explicit filename compatibility

Two reproducible fixtures are preserved under test data:

```text
TestData/ExplicitFilenameCompatibility/
TestData/ExplicitFilenameRemainingComponents/
```

The first fixture records seven hitobject cases covering explicit filename behavior and control cases.

Manual playback confirmed:

```text
osu!stable
    explicit filename -> custom sample only

osu!lazer
    explicit filename -> custom sample plus applicable additions
```

The project deliberately uses the legacy/osu!stable custom-only result as its canonical logical model.

`GetHitSoundLayers(HitObject)` now returns only `Custom` when `HitSample.Filename` is present while preserving the parsed source fields.

The second fixture covers spinner and slider behavior. Manual testing plus the official `ppy/osu` legacy parser confirmed that slider trailing `hitSample` handling is different from ordinary hitobjects:

```text
slider HitSample.NormalSet
slider HitSample.AdditionSet
    -> remain relevant to slider sample-bank resolution

slider HitSample.Index
slider HitSample.Volume
slider HitSample.Filename
    -> do not override legacy slider edge/body/tick sample resolution
```

Current implemented data flow:

```text
slider edges
    sample sets -> SliderEdge values
    SampleIndex / Volume -> sample timing point at edge time
    explicit slider HitSample.Filename -> ignored for edge generation

slider body
    sample sets -> slider HitSample NormalSet / AdditionSet
    SampleIndex / Volume -> sample timing point at slider start

slider ticks
    sample set -> slider HitSample NormalSet
    SampleIndex / Volume -> sample timing point at slider start
```

`GetSampleTimingPoint(double time)` resolves the sample timing point used for legacy sample values:

```text
active timing point at or before time
    -> use it

time is before every timing point
    -> use the first timing point in the map

no timing points exist
    -> caller uses documented defaults
       SampleIndex = 0
       Volume = 100
```

The lazer-specific spinner traversal sound observed during manual testing is intentionally deferred. It is not part of the current canonical legacy model.

### Remaining compatibility question

The interaction between slider `edgeSets = 0:0` and the slider-level `NormalSet` / `AdditionSet` still needs explicit verification against official behavior or a reproducible beatmap before changing code.

Do not guess this rule.

## Tooling checkpoint

A repository `.editorconfig` is present locally to align VS Code/C# naming diagnostics and suggestions with the project's established naming conventions.

## Refactoring status

There is visible duplication among standard, slider-body, and slider-tick filename lookup methods.

Do not refactor it yet.

`GetHitSoundLayers(SliderEdge, HitSample, double time)` currently retains the `HitSample` parameter even though legacy slider edge resolution no longer uses its `Index`, `Volume`, or `Filename`. Revisit that public signature only during a deliberate boundary/API review; do not mix it into the next behavior investigation.

## Deferred work

Do not mix these into the current compatibility investigation unless they become blockers.

- Define stable logical `SampleId` only after physical-resolution semantics are trustworthy.
- Audio decoding/playback belongs to the later Audio Infrastructure stage.
- Export and round-trip equivalence come before global optimization.
- Do not introduce CP-SAT or another global optimizer yet.
- Use `CultureInfo.InvariantCulture` for `.osu` numeric parsing when parser robustness work resumes.
- Improve malformed/empty parser field handling later.

## Exact next task

Verify the legacy semantics of slider `edgeSets = 0:0`, specifically whether zero-valued edge sample sets inherit from the slider-level `NormalSet` / `AdditionSet` or directly from the active sample timing point. Use official osu! documentation / official `ppy/osu` implementation and, if needed, a reproducible beatmap fixture before changing production code.

## Source-of-truth order

When sources disagree, use this order:

1. Tests and actual source code
2. Official osu! documentation / official open-source implementation for format behavior
3. `docs/DECISIONS.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/ROADMAP.md`
6. Chat history or AI suggestions
