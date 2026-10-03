# PROJECT_CONTEXT

Last updated: 2026-10-02

## Project

`osu-hitsound-editor`

C#/.NET learning project focused on building an osu!standard hitsound editor with a DAW-like workflow.

The project is developed incrementally. Core osu! behavior is implemented and verified before UI, export, and optimization are layered on top.

---

## Current stage

**Public Repository Readiness — COMPLETE**

The previous functional stage, **Hitsound Resolution**, is complete.

Current test status:

```text
100/100 passing
```

The pre-publication review is complete.

The next functional stage is:

```text
Physical Sample Resolution
```

Do not begin new functional work until the current publication checkpoint has been committed, pushed, synchronized to the Drive mirror, and the first GitHub Actions test run has been verified.

---

## Current objective

Close the completed public-repository readiness checkpoint.

Immediate operational steps:

- commit the reviewed source, tests, documentation, `CONTRIBUTING.md`, `LICENSE`, and GitHub Actions workflow
- push `main`
- verify the first GitHub Actions `Tests` workflow run
- synchronize the continuity documents to the read-only Drive mirror
- make the repository public after the pushed checkpoint and CI result are verified

After publication, begin **Physical Sample Resolution**.

---

## Functional implementation checkpoint

### Beatmap model

`Beatmap` currently includes data from these `.osu` sections:

- `[General]`
- `[Editor]`
- `[Metadata]`
- `[Difficulty]`
- `[TimingPoints]`
- `[HitObjects]`

It also stores the parsed osu! file format version through:

```text
BeatmapVersion
```

Relevant properties include:

- `BeatmapVersion`
- `AudioFilename`
- `AudioLeadIn`
- `PreviewTime`
- `Countdown`
- `DefaultSampleSet`
- `Mode`
- `Bookmarks`
- `DistanceSpacing`
- `BeatDivisor`
- `GridSize`
- `TimelineZoom`
- `Title`
- `Artist`
- `Creator`
- `Difficulty`
- `HPDrainRate`
- `CircleSize`
- `OverallDifficulty`
- `ApproachRate`
- `SliderMultiplier`
- `SliderTickRate`
- `TimingPoints`
- `HitObjects`

---

## Timing resolution

Implemented in `Beatmap`:

```text
GetActiveTimingPoint(double time)
GetActiveUninheritedTimingPoint(double time)
GetActiveInheritedTimingPoint(double time)
GetEffectiveSliderVelocityMultiplier(double time)
```

Current slider velocity behavior:

```text
if there is no active inherited timing point
    SV = 1.0

otherwise
    SV = 100 / -BeatLength
```

Timing methods use `double` because slider repeat, tail, and tick times may occur at fractional milliseconds.

---

## Slider timing

Section in `Beatmap.cs`:

```text
SLIDERS - TIMING
```

Implemented:

```text
GetSliderSpanDuration(HitObject slider)
GetSliderDuration(HitObject slider)
GetSliderEdgeTime(HitObject slider, int edgeIndex)
GetSliderTickDistance(HitObject slider)
GetSliderTickTimes(HitObject slider)
```

### Span duration

```text
spanDuration =
    slider.Length
    / (SliderMultiplier * 100 * SV)
    * activeUninheritedTimingPoint.BeatLength
```

### Total duration

```text
sliderDuration =
    spanDuration * slider.Slides
```

### Edge time

```text
edgeTime =
    slider.Time
    + spanDuration * edgeIndex
```

Edge interpretation:

```text
edgeIndex = 0               -> head
edgeIndex = 1..Slides - 1   -> repeats
edgeIndex = Slides          -> tail
```

`GetSliderSpanDuration` rejects a non-positive `SliderMultiplier`.

`GetSliderEdgeTime` validates the requested edge index.

---

## Slider edge hitsounds

Implemented:

```text
GetSliderEdgeHitSoundLayers(HitObject slider, int edgeIndex)
```

Flow:

```text
calculate edgeTime:
    GetSliderEdgeTime(slider, edgeIndex)

validate:
    SliderEdges.Count == Slides + 1

get:
    slider.SliderEdges[edgeIndex]

resolve:
    GetHitSoundLayers(
        sliderEdge,
        slider.HitSample,
        edgeTime
    )
```

Important verified behavior:

A repeat or tail resolves `SampleSet`, `SampleIndex`, and `Volume` using the timing point active at the edge's actual time, not necessarily the timing point active when the slider started.

---

## Slider body hitsounds

Implemented:

```text
GetSliderBodyHitSoundLayers(HitObject slider)
```

Behavior:

- Every slider produces a `SliderSlide` layer.
- `SliderSlide` uses the effective normal sample set.
- A slider with whistle also produces a `SliderWhistle` layer.
- `SliderWhistle` uses the effective addition sample set.
- Both use the effective sample index and volume.
- `Finish` and `Clap` do not create continuous slider-body layers.

---

## Slider ticks

Implemented:

```text
GetSliderTickDistance(HitObject slider)
GetSliderTickTimes(HitObject slider)
GetSliderTickHitSoundLayers(HitObject slider)
```

### Tick distance

Base tick distance:

```text
SliderMultiplier * 100 / SliderTickRate
```

Beatmap-version behavior:

```text
BeatmapVersion < 8
    tickDistance = baseTickDistance

BeatmapVersion >= 8
    tickDistance = baseTickDistance * effectiveSV
```

`GetSliderTickDistance` rejects:

```text
SliderTickRate <= 0
SliderMultiplier <= 0
```

### Tick times

Current behavior includes:

- ticks generated per slider span
- reversed timing behavior on odd spans
- chronological output
- 10 ms end exclusion
- support for multiple spans

### Tick hitsound

`GetSliderTickHitSoundLayers` produces one logical:

```text
SliderTick
```

The tick uses:

```text
effective normal sample set
effective sample index
effective volume
```

It does not use the slider's addition sample set.

---

## Existing hitsound resolution

Implemented functionality includes:

- effective `SampleIndex`
- effective `Volume`
- effective normal sample set
- effective addition sample set
- beatmap `DefaultSampleSet`
- `HitSoundType` bit flags
- logical `HitSoundLayer` generation
- custom filename handling
- slider edge sample resolution
- slider body logical layers
- slider tick logical layers

Relevant overload:

```text
GetHitSoundLayers(
    SliderEdge sliderEdge,
    HitSample hitSample,
    double time
)
```

`HitSoundType` currently includes:

```text
Normal
Whistle
Finish
Clap
Custom
SliderSlide
SliderWhistle
SliderTick
```

---

## Current code organization in Beatmap.cs

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

Do not create new sections, helper classes, resolver classes, or abstractions unless they solve a real current problem.

---

## Important current design decisions

### Logical and physical sample identity remain separate

A logical sample describes the sound the editor wants to use.

The following values are part of osu!'s physical representation and are not automatically the identity of the logical sound:

```text
SampleSet
SampleIndex
Filename
```

This separation must remain intact as Physical Sample Resolution is implemented.

### No combined slider-event API yet

Do not introduce a method such as:

```text
GetAllSliderHitSounds()
```

at the current stage.

Slider edges, continuous body sounds, and ticks have different timing and behavior.

A combined API should only be introduced when a concrete consumer such as the timeline requires an event abstraction containing the necessary timing and hitsound information.

### No global optimizer yet

Do not introduce CP-SAT or another global optimizer until this deterministic cycle works:

```text
parse
-> logical representation
-> export
-> reload
-> logical equivalence verification
```

Prefer a deterministic solution first.

---

## Test checkpoint

Current verified status:

```text
100/100 passing
```

Coverage now includes:

- parser settings and metadata
- `BeatmapVersion`
- timing inheritance
- inherited/uninherited timing points
- slider velocity
- effective sample sets
- sample index
- volume
- logical hitsound layers
- slider span duration
- slider total duration
- real slider edge times
- slider edge hitsounds
- slider body hitsounds
- slider tick distance
- pre-v8 / v8+ tick behavior
- slider tick times
- reversed spans
- slider tick hitsound layers
- relevant invalid states
- boundary cases

Tests remain the primary verification mechanism.

Never modify a correct test only to make an incorrect implementation pass.

---

## Naming cleanup

The old property typo:

```text
StarstNewCombo
```

has been corrected to:

```text
StartsNewCombo
```

Do not reintroduce the old name.

---

## Publication-readiness checkpoint

Pre-publication review completed:

- `.gitignore` reviewed
- no tracked local Windows paths found
- no obvious tracked credentials/secrets found
- synthetic test beatmap reviewed for redistribution safety
- known `StartsNewCombo` typo fixed
- README rewritten and updated
- `CONTRIBUTING.md` added and reviewed
- MIT `LICENSE` added
- `docs/architecture.md` updated
- `docs/ROADMAP.md` updated
- `docs/DECISIONS.md` updated
- `docs/AI_WORKFLOW.md` updated
- `docs/WORKFLOW.md` updated
- `docs/PROJECT_CONTEXT.md` updated
- numbered `Phase N` terminology removed from project documentation
- GitHub repository About and topics reviewed
- GitHub repository features and security settings reviewed
- GitHub Actions test workflow added at `.github/workflows/tests.yml`
- final local regression completed: `100/100 passing`
- `git diff --check` clean except for expected LF -> CRLF warnings on Windows
- final repository status and diff review completed

Operational publication steps still required:

```text
commit
push
verify first GitHub Actions Tests run
sync project-context documents to Drive mirror
make repository public
```

The repository-publication milestone is separate from the later osu!tools submission milestone.

---

## Physical Sample Resolution preparation

The next functional stage is **Physical Sample Resolution**.

The stage must preserve osu! sample lookup semantics instead of reducing physical resolution to filename generation alone.

Important distinction:

```text
SampleIndex = 0
```

does not participate in beatmap custom-sample lookup in the same way as:

```text
SampleIndex = 1
```

even when both may correspond to the same base filename.

Physical sample resolution must be able to distinguish between:

- sample found in the beatmap
- external fallback required
- explicit custom filename found
- explicit custom filename missing

This behavior is recorded as architectural decision `D017`.

Do not introduce `SampleId`, audio metadata, optimization, or a global resolver abstraction before the required lookup behavior is clear and tested.

---

## Next functional stage

### Physical Sample Resolution

Planned responsibilities:

```text
discover sample files used by the beatmap
resolve osu! sample filenames
separate logical sample identity from physical .osu representation
define a stable logical SampleId
load sample metadata required by the editor
handle missing samples predictably
add sample-resolution tests
```

Do not introduce sample optimization during this stage.

---

## Later submission milestone

The project should eventually be evaluated for submission to:

```text
https://tools.osuck.net/submit
```

The intended evaluation point is after:

```text
Export and Round-Trip Verification
```

By that point the project should provide a complete useful workflow for another user, including opening, editing, previewing, exporting, and verifying a beatmap.

Before submission, re-check the current osu!tools submission requirements rather than relying on old assumptions.

Sample Optimization is not currently considered a mandatory requirement for the first osu!tools submission.

---

## Next task

After the publication checkpoint has been pushed and GitHub Actions has been verified, begin **Physical Sample Resolution** by defining and testing the smallest deterministic behavior required to preserve osu! sample lookup semantics.

Start with standard hitsound lookup behavior before introducing file-system resolution, `SampleId`, audio metadata, or optimization.

---

## Technical debt / deferred work

Do not mix these into unrelated functional changes.

- Use `CultureInfo.InvariantCulture` consistently for `.osu` numeric parsing.
- Improve parser robustness for malformed or empty fields.
- Validate slider edge data more broadly when parser validation is addressed.
- Re-evaluate extreme slider/SV clamping behavior only if real requirements justify it.
- Add broader real-world beatmap coverage later, using redistributable or synthetic test data.

---

## Documentation terminology

Roadmap stages should use descriptive names instead of numbered phases.

Current sequence:

```text
Beatmap Parsing and Base Model
Hitsound Resolution
Physical Sample Resolution
Audio Infrastructure
Desktop Editor and Timeline
Export and Round-Trip Verification
Sample Optimization
Integration and Release Polish
```

Use these names consistently in project documentation.

---

## Source-of-truth order

When sources disagree, use this order:

1. Tests and actual source code
2. Official osu! documentation / official open-source implementation for format behavior
3. `docs/DECISIONS.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/ROADMAP.md`
6. Chat history or AI suggestions
