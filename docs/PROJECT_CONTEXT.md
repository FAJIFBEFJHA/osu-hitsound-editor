# PROJECT_CONTEXT

## Project

`osu-hitsound-editor`

C#/.NET learning project focused on building an osu!standard hitsound editor with a DAW-like workflow.

## Current phase

**Phase 2 — Real hitsound resolution**

Current test status:

```text
82/82 passing
```

## Current objective

Connect slider edge timing with hitsound resolution so each slider edge resolves its samples using the timing point active at the edge's real time.

## Current implementation checkpoint

### Beatmap model

`Beatmap` currently includes data from these `.osu` sections:

- `[General]`
- `[Editor]`
- `[Metadata]`
- `[Difficulty]`
- `[TimingPoints]`
- `[HitObjects]`

Relevant properties include:

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

### Timing resolution

Implemented in `Beatmap`:

- `GetActiveTimingPoint(double time)`
- `GetActiveUninheritedTimingPoint(double time)`
- `GetActiveInheritedTimingPoint(double time)`
- `GetEffectiveSliderVelocityMultiplier(double time)`

Slider velocity rule currently used:

```text
if there is no active inherited timing point:
    SV = 1.0

otherwise:
    SV = 100 / -BeatLength
```

### Slider timing

Section:

```text
SLIDERS - TIMING
```

Implemented:

- `GetSliderSpanDuration(HitObject slider)`
- `GetSliderDuration(HitObject slider)`
- `GetSliderEdgeTime(HitObject slider, int edgeIndex)`

Current behavior:

```text
spanDuration =
    slider.Length
    / (SliderMultiplier * 100 * SV)
    * activeUninheritedTimingPoint.BeatLength

sliderDuration =
    spanDuration * slider.Slides

edgeTime =
    slider.Time
    + spanDuration * edgeIndex
```

`GetSliderEdgeTime` treats:

```text
edgeIndex = 0              -> head
edgeIndex = 1..Slides - 1  -> repeats
edgeIndex = Slides         -> tail
```

### Slider edge hitsounds

Implemented:

- `GetSliderEdgeHitSoundLayers(HitObject slider, int edgeIndex)`

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

A repeat/tail resolves `SampleSet`, `SampleIndex`, and `Volume` using the timing point active at that edge's actual time, not necessarily the timing point active when the slider started.

### Slider body hitsounds

Implemented:

- `GetSliderBodyHitSoundLayers(HitObject slider)`

Behavior:

- Every slider produces a `SliderSlide` layer.
- `SliderSlide` uses the effective normal sample set.
- A slider with whistle also produces a `SliderWhistle` layer.
- `SliderWhistle` uses the effective addition sample set.
- Both use the effective sample index and volume.
- Finish and Clap do not create continuous slider body layers.

### Existing hitsound resolution

Implemented functionality includes:

- effective `SampleIndex`
- effective `Volume`
- effective normal/addition sample sets
- beatmap `DefaultSampleSet`
- `HitSoundType` bit flags
- logical `HitSoundLayer` generation
- custom filename handling
- slider edge sample resolution

Relevant overload:

```text
GetHitSoundLayers(
    SliderEdge sliderEdge,
    HitSample hitSample,
    double time
)
```

## Current code organization in Beatmap.cs

```text
PROPERTIES

HELPERS DE RESOLUCIÓN

TIMING
    GetActiveTimingPoint
    GetActiveUninheritedTimingPoint
    GetActiveInheritedTimingPoint
    GetEffectiveSliderVelocityMultiplier

SLIDERS - TIMING
    GetSliderSpanDuration
    GetSliderDuration
    GetSliderEdgeTime

SAMPLE SETS

HITOBJECT - VALORES EFECTIVOS

SLIDER EDGE - VALORES EFECTIVOS

HITSOUND TYPES

HITSOUND LAYERS
    GetHitSoundLayers(HitObject)
    GetHitSoundLayers(SliderEdge, HitSample, double time)
    GetSliderEdgeHitSoundLayers
```

Do not create new sections/classes/helpers unless they provide a real benefit.

## Next task

Continue Phase 2 from the `82/82` checkpoint.

Review the remaining slider-specific audio behavior, especially slider ticks, and determine what must be represented during Phase 2 versus what can wait until later audio/playback phases.

Do not introduce a new abstraction until the required behavior is clear.

## Technical debt / deferred work

Do not mix these into unrelated functional changes.

- Use `CultureInfo.InvariantCulture` for `.osu` numeric parsing.
- Improve parser robustness for malformed/empty fields.
- Verify/fix the possible `StarstNewCombo` typo if still present.
- Validate slider edge data more broadly when parser validation is addressed.
- Rewrite/review README before making the repository public.
- Add an appropriate license before publication.
- Review repository for personal paths, secrets, copyrighted beatmaps/audio, or other files that should not be public.

## Public repository checkpoint

The repository should become public at the **beginning of Phase 3**, after Phase 2 is complete and a publication-readiness review passes.

## Source-of-truth order

When sources disagree, use this order:

1. Tests and actual source code
2. Official osu! documentation / official open-source implementation for format behavior
3. `docs/DECISIONS.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/ROADMAP.md`
6. Chat history or AI suggestions
