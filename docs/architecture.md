# Architecture

This document describes the current architecture of **osu-hitsound-editor**.

Its purpose is to provide a visual reference for how `.osu` data is parsed, how timing and hitsound values are resolved, and how the current logical representation is separated from the physical sample representation that osu! uses.

> The architecture will continue to evolve as the project grows. This document describes the current implementation, not a final design.

---

## Current Stage

Parsing, logical hitsound resolution, and the current physical sample-resolution boundary are complete. The project is in **Audio Infrastructure**.

Current verified checkpoint:

```text
767/767 automated tests passing (full suite, 2026-10-29 via `New-SessionReview.ps1`)
```

The audio engine supports decoding, normalized mixing, timeline seek, rendered-position reporting, and relative source delays through `OffsetSampleProvider`. `Beatmap` enumerates chronological discrete hitsound events, and `TimedHitSoundPlaybackCursor` tracks selection progress. `HitSoundPlaybackService` can dispatch discrete events immediately or prequeue a bounded interval before playback using each event's time relative to the interval origin. The prequeue path starts finite looping slider-body layers at included slider heads. `QueuePlaybackInterval(...)` also reconstructs bodies already active at the interval start, preserving sample phase and playing only their remaining duration, with reconstruction results separate from discrete-event results. `FrameScheduledSampleProvider` sits in the WASAPI output path and provides partial absolute-frame scheduling through segmented mixer reads.

Prequeueing is verified against mixer frames and timeline audio. The WASAPI connection passed one temporary integration test and audible playback; that test was removed. Absolute-frame scheduling during active playback remains unverified. Active-body reconstruction is implemented for bounded prequeueing; playback-controller coordination, editor UI, export, and optimization remain future work.

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
- `AudioPlaybackEngine` owns the output device, timeline source, mixer, and transient audio-reader lifetimes, including finite looping sources.
- `FrameScheduledSampleProvider` wraps the persistent mixer between `WasapiPlayer` and `MixingSampleProvider`, counts frames returned by `Read(...)`, and activates queued sample providers at absolute frame boundaries.
- `LoopingWaveStream` repeats reads by rewinding a seekable `WaveStream`; it does not own or dispose the underlying reader.
- `HitSoundPlaybackService` dispatches discrete hitsound layers and resolves/queues continuous slider-body layers through `PlaySliderBody(...)`; `QueuePlaybackInterval(...)` combines head-triggered playback with reconstruction of bodies already active at the interval start, keeping their results separate.
- `TimedHitSoundEvent` stores an event time, `HitObject`, `HitSoundEventType`, and optional `EdgeIndex`.
- `TimedHitSoundPlaybackCursor` tracks discrete event progress and can reset its position after seeking.
- `HitCirclePlaybackCursor` separately tracks hit-circle event progress and can reset its position after seeking.

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
        +GetActiveSlidersAt(time) List~HitObject~
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

`HitSoundPlaybackService.PlaySliderBody(...)` obtains the slider's total duration from `Beatmap.GetSliderDuration(slider)` and its logical body layers from `GetSliderBodyHitSoundLayers(slider)`. Its optional `elapsedMilliseconds` must be finite, non-negative, and less than the total slider duration. The service calculates `remainingDurationMilliseconds = durationMilliseconds - elapsedMilliseconds`; each layer is resolved through `SampleResolver`, and an available local file is passed to `AudioPlaybackEngine.AddLoopingAudioSource(...)` with the remaining duration, layer volume, optional relative delay, and `sourceOffsetMilliseconds: elapsedMilliseconds`. Missing custom samples and external fallbacks are returned as resolution outcomes, not sent to the mixer. Invalid slider state or duration is rejected.

`AddLoopingAudioSource(...)` uses `LoopingWaveStream`, normalized sample providers, volume, and an `OffsetSampleProvider` duration limit. The audio engine owns the playback readers; the looping wrapper does not dispose its underlying reader. In `QueueTimedHitSoundEventsInRange(...)`, the body is started only when the slider-head event is included; repeat/tail events do not restart it. Active bodies can also be reconstructed separately at an interval start.

`AddLoopingAudioSource(...)` accepts an optional `sourceOffsetMilliseconds` independent of the requested playback duration and prequeue delay. The engine converts elapsed source time to native source frames, calculates its position within the sample, and assigns a frame-aligned byte position to `LoopingWaveStream` before normalization. The default offset of zero preserves existing head-triggered behavior. `PlaySliderBody(...)` uses the offset and remaining duration when given elapsed slider time.

`Beatmap.GetActiveSlidersAt(...)` identifies sliders whose bodies satisfy `slider.Time < time < slider.Time + GetSliderDuration(slider)`. Exact head and tail instants are excluded. `HitSoundPlaybackService.QueueActiveSliderBodiesAt(...)` selects those sliders and calls `PlaySliderBody(...)` with elapsed time since each slider's start, preserving sample phase and queuing only the remaining body duration. It does not synthesize or replay discrete head events.

`HitSoundPlaybackService.QueuePlaybackInterval(...)` combines reconstruction at the interval start with `QueueTimedHitSoundEventsInRange(...)`. Reconstructed continuous-body results remain separate from discrete-event results. A slider starting exactly at the interval boundary is excluded from active-body reconstruction; if its head is included by the discrete interval rules, the existing head-triggered path starts its body once. This avoids duplicate head playback or duplicate body starts.

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
    C --> T[Active slider body selection]
    T --> P
    N --> P
    P --> Q[AudioPlaybackEngine mixer]
    W[WasapiPlayer] --> V[FrameScheduledSampleProvider]
    V --> Q
    V -. future .-> R[Active-playback scheduling / editor UI]
    C -. future .-> S[Export / optimization]
```

The audio path can dispatch selected discrete hitsounds immediately or prequeue discrete events together with slider-body playback triggered at included slider heads and reconstructed bodies already active at the interval start, before the mixer starts consuming audio. Prequeueing converts each absolute event time into a delay relative to one common interval start. `AudioPlaybackEngine.InitializeOutput()` connects `WasapiPlayer` → `FrameScheduledSampleProvider` → `MixingSampleProvider`; the scheduler activates queued sources at absolute frames as WASAPI reads. The dashed branches indicate future active-playback scheduling behavior, editor, export, and optimization functionality.

---

## Current Architectural Boundary

The current implementation reaches discrete hitsound playback, finite slider-body playback, active-body reconstruction, bounded interval prequeueing, and a frame scheduler connected to the production output path without implementing an editor timeline or verified absolute-frame scheduling during active playback.

Implemented:

```text
.osu parsing -> logical HitSoundLayer -> SampleResolver
            -> SampleResolutionResult -> mixer input (if found)

Beatmap -> GetTimedHitSoundEventsInRange(...) -> timed discrete events
TimedHitSoundEvent -> PlayTimedHitSoundEvent(...) -> immediate dispatch or optional delay
TimedHitSoundPlaybackCursor -> AdvanceTo(...) / SeekTo(...) -> event selection progress
QueueTimedHitSoundEventsInRange(...) -> per-event relative delay -> prequeued mixer inputs
Slider-head event (EdgeIndex = 0) -> PlaySliderBody(...) -> finite looping mixer inputs
Beatmap -> GetActiveSlidersAt(...) -> QueueActiveSliderBodiesAt(...) -> remaining body playback
QueuePlaybackInterval(...) -> active-body reconstruction + discrete interval queueing
                           -> separate reconstruction and discrete-event results
FrameScheduledSampleProvider.Schedule(source, absoluteFrame) -> priority queue
FrameScheduledSampleProvider.Read(...) -> segmented mixer reads + event activation
                                     -> FramesRead counts returned complete frames
WasapiPlayer -> FrameScheduledSampleProvider -> MixingSampleProvider
```

`AudioPlaybackEngine.AddAudioSource(...)` accepts an optional finite, non-negative `delayMilliseconds`; `OffsetSampleProvider` inserts initial silence in whole mixer frames. The mixer-input provider is also the key used to release its underlying reader when playback ends.

`QueueTimedHitSoundEventsInRange(...)` selects a chronological interval and computes `event.Time - startMilliseconds`. By default the interval is `(start, end]`, except that a start at zero includes events at zero. An optional `includeStart` includes an event at a positive start boundary, supporting post-seek reconstruction. When an included event is a slider head (`SliderEdge`, `EdgeIndex = 0`), the queue also calls `PlaySliderBody(...)` with the same delay and appends its resolution results to those of the head. Repeats and tails remain discrete-only events. Sources must be queued **before** the mixer advances through the interval; adding them after playback has progressed does not retrospectively align their timestamps. This method alone does not reconstruct the remaining body when an interval begins midway through a slider; `QueuePlaybackInterval(...)` adds that reconstruction through `QueueActiveSliderBodiesAt(...)`.

Focused device tests confirmed pause/resume, pause-seek-requeue-play, and stop/requeue with a loaded timeline. These are integration checks, not permanent device-dependent suite tests. Current `Stop()` has a separate limitation when no timeline audio is loaded.

`FrameScheduledSampleProvider` queues sample providers by absolute frame and insertion order. `Read(...)` splits mixer reads at queued frame boundaries, adds due sources to the mixer, and advances `FramesRead` by complete frames returned. `Read(...)` and `Schedule(...)` share `_scheduleLock`; reentrant scheduling during a read is rejected. An internal read-processing failure faults the scheduler, so later reads and schedules reject it. The 23 initial permanent tests covered standalone frame scheduling; the full suite now has 767 permanent tests. The WASAPI connection passed one temporary integration test and audible playback, then the temporary test was removed. Complete pending/active source ownership, cleanup, and recovery remain open.

Not verified yet: absolute-frame scheduling while audio is actively advancing, the origin of scheduled frames, coordination with `Seek(...)` and `Stop()`, complete pending/active source management, and recovery after internal failures. Playback-controller coordination of pause and reconstruction, editor timeline/UI, deterministic export/reload/equivalence verification, and sample optimization also remain future work.

Earlier investigation used 16 temporary diagnostic checks: seven for frame accounting and nine for buffer-boundary scheduling and controlled concurrency. Their files were removed; they are separate from the permanent suite and do not establish concurrency guarantees for `FrameScheduledSampleProvider`.

Future playback must support reduced speeds and `SamplesMatchPlaybackRate` behavior. The behavior still needs to be defined and verified; no implementation or technical approach is established here.

---

## Tests

The project uses **xUnit** for automated testing.

Current checkpoint:

```text
767/767 tests passing (full suite, 2026-10-29 via `New-SessionReview.ps1`)
```

Historical milestones remain: 669/669 on 2026-10-14, 681/681 on 2026-10-18, and 741/741 on 2026-10-21. The three active-slider selection and interval-reconstruction methods added 60 permanent cases, bringing the suite from 681 to 741. The initial 23 `FrameScheduledSampleProvider` tests brought it to 764; the current suite has 767. The 16 removed scheduler diagnostics and the removed one-test WASAPI check are separate from this permanent suite.

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
unified TimedHitSoundPlaybackCursor advancement/seek
relative delayed playback and mixer-reader cleanup
finite looping sample playback, duration cutoff, volume, delays, and reader cleanup
source-offset validation and frame-aligned loop phase with wraparound and finite cutoff
prequeued interval playback, inclusive boundaries, and overlapping hitsounds
shared-origin mixing of prequeued hitsounds with timeline audio
finite slider-body playback, head/body delay alignment, whistle mixing and missing-sample outcomes
partial slider-body playback with preserved sample phase, remaining-duration cutoff, optional delay and elapsed-time validation
active-slider selection excluding exact head/tail boundaries
active-body reconstruction with preserved phase and remaining duration
combined playback intervals with separate body/event results and no duplicate head playback
absolute-frame scheduling with segmented reads, event activation, frame accounting, serialized reads/schedules, reentrancy rejection, and fail-stop behavior
```

The tests are located under:

```text
tests/
└── OsuHitsoundEditor.Tests/
    ├── BeatmapTests.cs
    ├── BeatmapLoaderTests.cs
    ├── TestOrganizationTests.cs
    ├── HitCirclePlaybackCursorTests.cs
    ├── TimedHitSoundPlaybackCursorTests.cs
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

Implemented: NAudio decoding, output/mixer lifetime, timeline playback and seek, per-source volume, chronological event selection with `TimedHitSoundPlaybackCursor`, relative delayed sample playback, slider-body layers with finite looping, active-body reconstruction with preserved phase and remaining duration, bounded interval prequeueing with separate reconstruction/discrete-event results before the mixer advances, and `FrameScheduledSampleProvider` in the WASAPI output path for absolute-frame activation, serialized reads/schedules, and fail-stop processing. Focused real-device checks covered pause/resume, seek/requeue, stop/requeue, and the scheduler's WASAPI connection.

Remaining: verification of absolute-frame scheduling during active playback, the scheduled-frame origin, coordination with `Seek(...)` and `Stop()`, complete ownership/cleanup of pending and active sources, recovery after internal failure, playback-controller coordination, reduced playback speeds and verified `SamplesMatchPlaybackRate` behavior, and editor-timeline integration. NAudio is the selected external audio infrastructure.

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
