# Architecture

This document describes the current architecture of **osu-hitsound-editor**.

Its purpose is to provide a visual reference for how `.osu` data is parsed, how timing and hitsound values are resolved, and how the current logical representation is separated from the physical sample representation that osu! uses.

> The architecture will continue to evolve as the project grows. This document describes the current implementation, not a final design.

---

## Current Stage

Parsing, logical hitsound resolution, and the current physical sample-resolution boundary are complete. The project is in **Audio Infrastructure**.

Verified checkpoint (2026-10-11):

```text
500/500 automated tests passing (full suite)
```

The audio engine supports decoding, normalized mixing, timeline seek, and rendered-position reporting. `HitSoundPlaybackService` resolves and immediately adds discrete hit-circle, slider-edge, and slider-tick samples to the mixer. `Beatmap` can enumerate these events chronologically through `TimedHitSoundEvent`.

Precise scheduling, continuous slider-body audio, editor UI, export, and optimization remain future work. The presence of a timestamp on an event does not yet imply scheduled playback.

---

## Current Architecture Overview

The diagram below shows the core parsing and logical-resolution path. Physical sample lookup, discrete-event selection, and playback are described in the later sections.

```mermaid
flowchart LR
    A[.osu file] --> B[BeatmapLoader]
    B --> C[Beatmap]

    C --> D[TimingPoints]
    C --> E[HitObjects]

    E --> F[HitSample]
    E --> G[SliderEdges]

    D --> H[Timing resolution]
    F --> I[Hitsound resolution]
    G --> I
    H --> I

    I --> J[HitSoundLayer]
```

The main responsibilities are currently:

- `BeatmapLoader` parses `.osu` text into project objects.
- `Beatmap` owns beatmap-level data and the rules used to resolve timing and hitsounds.
- `TimingPoint` stores timing, sample-set, sample-index, and volume information.
- `HitObject` stores hit-circle, slider, and spinner data.
- `HitSample` stores the sample information attached to a hit object.
- `SliderEdge` stores hitsound information for the head, repeats, and tail of a slider.
- `HitSoundLayer` represents one logical sound layer after inheritance and timing rules have been resolved.
- `SampleResolver` maps logical layers to physical beatmap samples or explicit missing/fallback outcomes.
- `AudioPlaybackEngine` owns the output device, timeline source, mixer, and transient audio-reader lifetimes.
- `HitSoundPlaybackService` resolves hitsound layers and dispatches `HitCircle`, `SliderEdge`, and `SliderTick` playback immediately.
- `TimedHitSoundEvent` stores an event time, `HitObject`, `HitSoundEventType`, and optional `EdgeIndex`.
- `HitCirclePlaybackCursor` tracks hit-circle event progress and can reset its position after seeking.

Parsing, logical resolution, physical sample lookup, event selection, and audio output remain separate:

```text
.osu text -> BeatmapLoader -> Beatmap
Beatmap -> HitSoundLayer -> SampleResolver -> SampleResolutionResult
Beatmap -> TimedHitSoundEvent -> HitSoundPlaybackService
HitSoundPlaybackService -> AudioPlaybackEngine (found local samples only)
```

`SampleResolutionResult` can indicate a required external fallback or a missing custom file. Neither outcome is treated as an available local audio path.

---

## Current Class Structure

The diagram below intentionally shows the most relevant properties and methods rather than every member in every class.

```mermaid
classDiagram

    class Program {
        +Main()
    }

    class BeatmapLoader {
        +Load(path) Beatmap
        -ParseGeneralLine(...)
        -ParseEditorLine(...)
        -ParseMetadataLine(...)
        -ParseDifficultyLine(...)
        -ParseTimingPointLine(...)
        -ParseHitObjectLine(...)
        -ParseHitSample(...)
    }

    class Beatmap {
        +int BeatmapVersion
        +string Title
        +string Artist
        +string Creator
        +string Difficulty
        +int Mode
        +string AudioFilename

        +double SliderMultiplier
        +double SliderTickRate

        +List~TimingPoint~ TimingPoints
        +List~HitObject~ HitObjects

        +GetActiveTimingPoint(time) TimingPoint
        +GetActiveUninheritedTimingPoint(time) TimingPoint
        +GetActiveInheritedTimingPoint(time) TimingPoint
        +GetEffectiveSliderVelocityMultiplier(time) double

        +GetSliderSpanDuration(slider) double
        +GetSliderDuration(slider) double
        +GetSliderEdgeTime(slider, edgeIndex) double
        +GetSliderTickDistance(slider) double
        +GetSliderTickTimes(slider) List~double~

        +GetEffectiveSampleIndex(hitObject) int
        +GetEffectiveVolume(hitObject) int
        +GetEffectiveNormalSetType(hitObject) SampleSetType
        +GetEffectiveAdditionSetType(hitObject) SampleSetType

        +GetHitSoundTypes(hitObject) List~HitSoundType~
        +GetHitSoundLayers(hitObject) List~HitSoundLayer~
        +GetSliderEdgeHitSoundLayers(slider, edgeIndex) List~HitSoundLayer~
        +GetSliderBodyHitSoundLayers(slider) List~HitSoundLayer~
        +GetSliderTickHitSoundLayers(slider) List~HitSoundLayer~
    }

    class TimingPoint {
        +double Time
        +double BeatLength
        +bool IsUninherited
        +double Bpm
        +int SampleSet
        +int SampleIndex
        +int Volume
    }

    class HitObject {
        +int X
        +int Y
        +int Time
        +int ObjectType
        +int HitSound
        +double EndTime

        +HitSample HitSample
        +List~SliderEdge~ SliderEdges

        +int Slides
        +double Length

        +bool IsHitCircle
        +bool IsSlider
        +bool IsSpinner
        +bool StartsNewCombo
    }

    class HitSample {
        +int NormalSet
        +int AdditionSet
        +int Index
        +int Volume
        +string Filename
    }

    class SliderEdge {
        +int HitSound
        +int NormalSet
        +int AdditionSet
    }

    class HitSoundLayer {
        +HitSoundType Type
        +SampleSetType SampleSet
        +int SampleIndex
        +int Volume
        +string Filename
    }

    class SampleSetType {
        <<enumeration>>
        Default
        Normal
        Soft
        Drum
    }

    class HitSoundType {
        <<enumeration>>
        Normal
        Whistle
        Finish
        Clap
        Custom
        SliderSlide
        SliderWhistle
        SliderTick
    }

    Program --> BeatmapLoader : uses
    BeatmapLoader --> Beatmap : creates

    Beatmap "1" --> "*" TimingPoint : contains
    Beatmap "1" --> "*" HitObject : contains

    HitObject "1" --> "1" HitSample : contains
    HitObject "1" --> "*" SliderEdge : slider edges

    Beatmap --> HitSoundLayer : resolves
    HitSoundLayer --> SampleSetType : uses
    HitSoundLayer --> HitSoundType : uses
```

---

## Beatmap Loading Flow

`BeatmapLoader` reads the `.osu` file and builds the parsed model.

```mermaid
flowchart TD

    A[.osu file] --> B[BeatmapLoader.Load]
    B --> C[Read file lines]

    C --> D{Header or current section}

    D -->|osu file format vX| E[Set BeatmapVersion]
    D -->|General| F[Parse General settings]
    D -->|Editor| G[Parse Editor settings]
    D -->|Metadata| H[Parse Metadata]
    D -->|Difficulty| I[Parse Difficulty settings]
    D -->|TimingPoints| J[Create TimingPoint]
    D -->|HitObjects| K[Create HitObject]

    K --> L{Object type}

    L -->|HitCircle| M[Parse HitSample]
    L -->|Slider| N[Parse slider data and SliderEdges]
    L -->|Spinner| O[Parse EndTime and HitSample]

    E --> P[Beatmap]
    F --> P
    G --> P
    H --> P
    I --> P
    J --> P
    M --> P
    N --> P
    O --> P
```

The loader is responsible for reading the file representation.

It does not decide which logical samples should ultimately play. That responsibility belongs to the resolution methods in `Beatmap`.

---

## Timing Resolution

Timing behavior is resolved at the time being queried.

The current timing methods are:

```text
GetActiveTimingPoint(double time)
GetActiveUninheritedTimingPoint(double time)
GetActiveInheritedTimingPoint(double time)
GetEffectiveSliderVelocityMultiplier(double time)
```

The distinction between timing-point types is important:

```text
uninherited timing point
    ↓
base beat length / BPM timing

inherited timing point
    ↓
slider velocity and sample-related inheritance
```

When no active inherited timing point exists, the effective slider velocity multiplier is:

```text
SV = 1.0
```

Otherwise, the current rule is:

```text
SV = 100 / -BeatLength
```

The methods use `double` time values because slider repeat and tail times may occur at fractional milliseconds.

---

## Hitsound Inheritance

Some `.osu` sample values use `0` to mean that the effective value must be inherited.

The current resolution flow is:

```mermaid
flowchart TD

    A[HitObject / HitSample] --> D[Own value]
    B[Query object time] --> C[Active TimingPoint]
    C --> E[Inherited value]

    D --> F[Resolve effective value]
    E --> F

    F --> G[SampleIndex]
    F --> H[Volume]
    F --> I[NormalSet]
    F --> J[AdditionSet]
```

This resolution is used for:

```text
SampleIndex
Volume
NormalSet
AdditionSet
```

Sample-set inheritance can also fall back to the beatmap's default sample set when required.

---

## Sample Set Resolution

Numeric sample-set values from the `.osu` format are converted into `SampleSetType`.

```mermaid
flowchart LR

    A["0"] --> E[Default]
    B["1"] --> F[Normal]
    C["2"] --> G[Soft]
    D["3"] --> H[Drum]
```

The effective sample-set methods combine inheritance with this conversion:

```text
GetEffectiveNormalSetType()
GetEffectiveAdditionSetType()
```

The logical representation uses `SampleSetType` instead of carrying the raw integer everywhere.

---

## Logical Hitsound Resolution

`HitSound` uses bit flags that may be combined.

The base object hitsounds are:

```text
Normal
Whistle
Finish
Clap
```

`GetHitSoundTypes()` interprets those flags.

`GetHitSoundLayers()` then resolves the information required by each sound layer:

```mermaid
flowchart TD

    A[HitObject] --> B[HitSound flags]
    A --> C[HitSample]
    A --> D[Object time]

    B --> E[HitSoundType]
    C --> F[Effective SampleSet]
    C --> G[Effective SampleIndex]
    C --> H[Effective Volume]

    D --> I[TimingPoint lookup]
    I --> F
    I --> G
    I --> H

    E --> J[HitSoundLayer]
    F --> J
    G --> J
    H --> J
```

A `HitSoundLayer` describes the logical result:

```text
Type
SampleSet
SampleIndex
Volume
Filename
```

At this stage, the layer describes **what should sound logically**.

It does not yet guarantee that the corresponding physical sample file has been discovered or resolved from disk.

---

## Slider Timing

Slider timing is calculated in `Beatmap`.

### Span duration

Conceptually:

```text
spanDuration =
    slider.Length
    / (SliderMultiplier * 100 * SV)
    * activeUninheritedTimingPoint.BeatLength
```

`GetSliderSpanDuration()` requires:

- a valid positive `SliderMultiplier`
- an active uninherited timing point

Invalid states throw instead of returning valid-looking fallback values.

### Total duration

```text
sliderDuration =
    spanDuration * Slides
```

`Slides` represents the number of spans.

### Edge time

```text
edgeTime =
    slider.Time
    + spanDuration * edgeIndex
```

Edge indexes are interpreted as:

```text
0                 head
1 .. Slides - 1   repeats
Slides            tail
```

```mermaid
flowchart LR

    A[Slider start] --> B["edge 0: head"]
    B --> C["edge 1: repeat"]
    C --> D["..."]
    D --> E["edge Slides: tail"]
```

---

## Slider Edge Hitsounds

A slider may define different hitsound information for each edge.

The number of expected edges is:

```text
Slides + 1
```

`GetSliderEdgeHitSoundLayers()` first calculates the real time of the requested edge and then resolves that edge using the timing point active at that exact time.

```mermaid
flowchart TD

    A[Slider + edgeIndex] --> B[GetSliderEdgeTime]
    B --> C[edgeTime]

    A --> D[SliderEdges edgeIndex]
    C --> E[TimingPoint active at edgeTime]
    D --> F[Resolve edge hitsound]
    E --> F

    F --> G[List of HitSoundLayer]
```

This means a repeat or tail may inherit a different `SampleSet`, `SampleIndex`, or `Volume` from the slider head if a relevant timing point changes during the slider.

---

## Continuous Slider Body Hitsounds

The slider body has continuous sounds that are separate from the discrete head, repeat, tail, and tick events.

`GetSliderBodyHitSoundLayers()` resolves:

```text
SliderSlide
SliderWhistle
```

`SliderSlide` is always part of the continuous slider body.

`SliderWhistle` is added when the slider's whistle hitsound is enabled.

`Finish` and `Clap` are not treated as continuous slider-body sounds.

---

## Slider Ticks

Slider ticks have their own timing and logical hitsound behavior.

### Tick distance

The base tick distance is:

```text
baseTickDistance =
    SliderMultiplier * 100 / SliderTickRate
```

For beatmap formats before version 8:

```text
tickDistance = baseTickDistance
```

For version 8 and newer:

```text
tickDistance =
    baseTickDistance * effectiveSliderVelocityMultiplier
```

This distinction is represented explicitly through `BeatmapVersion`.

### Tick times

`GetSliderTickTimes()` calculates tick times for every slider span.

The current flow is:

```text
calculate span duration
calculate tick distance
calculate slider velocity
calculate the 10 ms end exclusion distance

for each span
    determine whether the span is reversed
    calculate ticks along the path
    convert path progress to time progress
    return ticks in chronological order
```

Ticks at or too close to the span end are not generated.

For reversed spans, path progress is inverted and the generated values are reordered so the returned list remains chronological.

### Tick hitsound

`GetSliderTickHitSoundLayers()` produces one logical layer:

```text
SliderTick
```

The tick uses the slider's effective normal sample set, sample index, and volume.

It does not use the slider's addition sample set.

---

## Slider Audio Overview

The slider audio responsibilities are currently separated like this:

```mermaid
flowchart TD

    A[Slider] --> B[Discrete edges]
    A --> C[Continuous body]
    A --> D[Ticks]

    B --> E[Head]
    B --> F[Repeats]
    B --> G[Tail]

    C --> H[SliderSlide]
    C --> I[SliderWhistle]

    D --> J[Tick times]
    D --> K[SliderTick]

    E --> L[HitSoundLayer]
    F --> L
    G --> L
    H --> L
    I --> L
    K --> L
```

These responsibilities remain separate because they occur at different times and follow different osu! rules.

---

## Current Data Flow

The current overall flow is:

```mermaid
flowchart TD

    A[.osu file] --> B[BeatmapLoader]
    B --> C[Parsed Beatmap]

    C --> D[TimingPoints]
    C --> E[HitObjects]

    E --> F[HitSample]
    E --> G[SliderEdges]

    D --> H[Timing resolution]

    F --> I[Effective sample values]
    G --> I
    H --> I

    E --> J[HitSound flags]

    I --> K[Logical hitsound resolution]
    J --> K

    K --> L[HitSoundLayer]

    L --> M[SampleResolver]
    M --> N[SampleResolutionResult]
    C --> O[TimedHitSoundEvent selection]
    O --> P[HitSoundPlaybackService]
    N --> P
    P --> Q[AudioPlaybackEngine mixer]
    Q -. future .-> R[Time-accurate scheduling / editor UI]
    C -. future .-> S[Export / optimization]
```

The audio path currently triggers selected hitsounds immediately. The dashed branches indicate future scheduling, editor, export, and optimization functionality.

---

## Current Architectural Boundary

The current implementation reaches discrete hitsound playback without implementing an editor timeline or time-accurate scheduler.

Implemented:

```text
.osu parsing -> logical HitSoundLayer -> SampleResolver
            -> SampleResolutionResult -> immediate mixer input (if found)

Beatmap -> GetTimedHitSoundEventsInRange(...) -> timed discrete events
TimedHitSoundEvent -> PlayTimedHitSoundEvent(...) -> appropriate playback method
```

`HitCirclePlaybackCursor` tracks only hit-circle progress; the unified event representation does not yet have a complete playback cursor. Range playback helpers dispatch all matching events immediately rather than delaying individual sounds until their timestamps.

Not implemented yet: precise audio-clock scheduling, continuous slider-body playback, editor timeline/UI, deterministic export/reload/equivalence verification, and sample optimization.

---

## Tests

The project uses **xUnit** for automated testing.

Current checkpoint:

```text
500/500 tests passing (full suite, 2026-10-11)
```

The test suite currently covers:

```text
.osu parsing
beatmap settings and metadata
BeatmapVersion parsing
TimingPoint parsing
active timing-point resolution
active inherited/uninherited timing points
slider velocity resolution
SampleSetType conversion
SampleIndex inheritance
Volume inheritance
NormalSet inheritance
AdditionSet inheritance
beatmap default sample set
HitSound flag combinations
logical HitSoundLayer resolution
slider span duration
slider total duration
slider edge timing
slider edge hitsounds
continuous SliderSlide
continuous SliderWhistle
slider tick distance
pre-v8 / v8+ tick behavior
slider tick times
normal and reversed slider spans
SliderTick hitsound layers
invalid-state and boundary cases
physical sample filenames, banked and universal fallback, explicit custom-file outcomes
audio opening, normalization, mixer control, timeline seeking and playback volume
hit-circle, slider-edge, and slider-tick playback and interval selection
hit-circle cursor advancement and seeking
chronological TimedHitSoundEvent generation and playback dispatch
```

The tests are located under:

```text
tests/
└── OsuHitsoundEditor.Tests/
    ├── BeatmapTests.cs
    ├── BeatmapLoaderTests.cs
    ├── TestOrganizationTests.cs
    ├── HitCirclePlaybackCursorTests.cs
    ├── HitSoundPlaybackServiceTests.cs
    ├── AudioPlaybackEngineTests.cs
    └── TestData/
        └── basic-beatmap.osu
```

Run the complete suite from the repository root with:

```bash
dotnet test
```

---

## Planned Development Stages

The following sections retain the original stage-level design goals. Physical sample lookup and substantial audio infrastructure are now implemented; remaining items are identified below. Refer to `docs/ROADMAP.md` for current task status.

### Physical Sample Resolution

Completed at the present lookup boundary: standard and slider sample filename resolution, banked/universal lookup, and explicit found/missing/fallback outcomes.

Deferred until needed: defining a stable logical `SampleId` and loading additional sample metadata.

### Audio Infrastructure

Implemented: NAudio decoding, output/mixer lifetime, timeline playback and seek, per-source volume, discrete hitsound dispatch, event-range selection, and chronological event representation.

Remaining: integration of chronological playback progress, accurate scheduled triggering, continuous slider-body audio, editor-timeline integration, and runtime device-error recovery. NAudio is the selected external audio infrastructure.

### Desktop Editor and Timeline

Planned responsibilities:

```text
WPF application shell
open beatmap
timeline
playhead
zoom
waveform
hitsound events and tracks
selection
editing
snapping
```

### Export and Round-Trip Verification

The exporter must eventually support this deterministic cycle:

```text
parse
    ↓
logical representation
    ↓
export
    ↓
reload
    ↓
logical equivalence verification
```

The equivalence check must verify relevant properties such as:

```text
samples
multiplicity
timing
volume
overlaps
```

This stage must work before global sample optimization is introduced.

### Sample Optimization

The intended optimization priority is:

```text
1. Minimize redundant physical sample files.
2. Among equally minimal solutions, minimize effective SampleSet/SampleIndex combinations.
3. Preserve logical and sonic behavior.
```

A deterministic approach should be preferred first.

A global optimizer such as CP-SAT should only be considered if the problem later demonstrates that it actually requires one.

### Integration and Release Polish

Later work includes:

```text
error handling
larger beatmap testing
performance profiling
usability review
packaging
documentation
release preparation
```

---

## Sample Optimization Goal

One of the long-term goals of the project is to avoid unnecessary duplication of sample files.

For example:

```text
drum-hitnormal2.wav
drum-hitnormal3.wav
drum-hitnormal4.wav
```

may all contain the same logical audio sample.

The future optimization system should reason about the logical sample first and only then decide how that sound should be represented through osu!'s physical file/index system.

Conceptually:

```mermaid
flowchart TD

    A[Logical samples] --> B[Determine sample identity]
    B --> C[Analyze required combinations]
    C --> D[Assign physical SampleSets and SampleIndexes]
    D --> E[Minimize duplicated files]
    E --> F[Generate osu-compatible representation]
```

The optimizer must not assume that two samples are equivalent simply because they sound similar.

Sample equivalence must be based on an explicit rule, such as identical audio content or an equivalence declared by the system or user.

---

## Development Principles

### Logical and physical sample identity remain separate

The project keeps these concepts separate:

```text
Logical sample
    ↓
The actual sound that should be used

Physical osu! representation
    ↓
SampleSet
SampleIndex
HitSound
Filename
```

`SampleSet`, `SampleIndex`, and filename are encoding choices. They are not automatically the identity of the logical sound.

### Core domain logic remains inside the project

The following areas are primarily implemented by this project:

```text
.osu parsing
osu! timing behavior
logical hitsound representation
export
sample optimization
```

External libraries are acceptable for infrastructure such as audio playback or generic UI components when they provide a real benefit.

### Avoid premature abstractions

New interfaces, helper classes, resolver classes, or architectural layers should not be introduced only because they might be useful later.

A new abstraction should solve a real problem that already exists.

### Tests are the primary verification mechanism

Functional behavior should be verified with tests before moving to the next responsibility.

An AI suggestion, documentation statement, or architectural idea does not override behavior that has already been verified by the source code and tests.

### Official osu! behavior is verified rather than guessed

When format or gameplay behavior is uncertain, use:

```text
1. official osu! documentation
2. official osu! open-source implementation
```

instead of assuming how the format behaves.
