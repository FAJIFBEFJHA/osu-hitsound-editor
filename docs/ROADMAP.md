# ROADMAP

## Goal

Build a Windows desktop hitsound editor for osu!standard with a DAW-like workflow while using the project to learn C#, .NET, architecture, testing, Git, and desktop application development.

The roadmap should remain incremental. Avoid large architectural jumps before the previous layer works deterministically.

---

## Beatmap Parsing and Base Model

**Status: COMPLETE**

Implemented foundations include:

- [x] `.osu` section parsing
- [x] File format version parsing
- [x] General settings
- [x] Editor settings
- [x] Metadata
- [x] Difficulty settings
- [x] Timing points
- [x] Hit objects
- [x] Hit samples
- [x] Sliders
- [x] Slider edges
- [x] Spinners
- [x] Hitsound flags
- [x] Base automated test suite

---

## Hitsound Resolution

**Status: COMPLETE**

Completed:

- [x] Resolve active timing point
- [x] Resolve active uninherited timing point
- [x] Resolve active inherited timing point
- [x] Resolve effective slider velocity multiplier
- [x] Resolve beatmap default sample set
- [x] Resolve effective normal sample set
- [x] Resolve effective addition sample set
- [x] Resolve sample index
- [x] Resolve volume
- [x] Resolve hitsound types
- [x] Build logical `HitSoundLayer` values
- [x] Calculate slider span duration
- [x] Calculate total slider duration
- [x] Calculate real slider edge times
- [x] Resolve slider edge hitsounds
- [x] Resolve slider edge samples using timing at the edge's real time
- [x] Resolve continuous slider body `SliderSlide`
- [x] Resolve continuous slider body `SliderWhistle`
- [x] Parse and use `BeatmapVersion`
- [x] Resolve slider tick distance
- [x] Preserve pre-v8 / v8+ slider tick behavior
- [x] Calculate slider tick times across normal and reversed spans
- [x] Resolve `SliderTick` hitsound layers
- [x] Validate relevant invalid slider states
- [x] Add boundary and regression coverage
- [x] Run full hitsound-resolution regression suite

### API decision

A combined `GetAllSliderHitSounds()`-style API is intentionally deferred.

The current slider responsibilities remain separate because head/repeat/tail events, continuous body sounds, and ticks have different timing and behavior. A combined event API should only be introduced when the timeline or another concrete consumer requires an event abstraction containing the necessary timing and hitsound information.

The Hitsound Resolution baseline was `100/100` tests beforhysical Sample Resolution began.

---

## Public repository milestone

**Status: COMPLETE**

Completed before/currently in Physical Sample Resolution:

- [x] All tests passed at publication checkpoint
- [x] Review/remove local-only paths
- [x] Review secrets
- [x] Review redistributed content
- [x] Verify `.gitignore`
- [x] Fix known naming/typo issues found during readiness work
- [x] Review README
- [x] Keep AI-mentorship disclosure
- [x] Add license
- [x] Publish repository

Repository:

```text
https://github.com/FAJIFBEFJHA/osu-hitsound-editor
```

---

## Physical Sample Resolution

**Status: IN PROGRESS**

Current full-suite checkpoint: **182/182 tests passing**

Completed:

- [x] Introduce `SampleResolver` as the physical sample lookup boundary
- [x] Resolve standard hitnormal/hitwhistle/hitfinish/hitclap lookup names
- [x] Resolve slider body `sliderslide` / `sliderwhistle` lookup names
- [x] Resolve slider tick `slidertick` lookup names
- [x] Preserve `SampleIndex = 0` vs `SampleIndex = 1` semantics
- [x] Resolve explicit custom filename existence in the beatmap directory
- [x] Verify beatmap sample extension order `.wav -> .mp3 -> .ogg`
- [x] Resolve supported beatmap sample extensions in that order
- [x] Represent physical resolution outcome explicitly
- [x] Distinguish beatmap sample found vs external fallback required
- [x] Distinguish explicit custom sample found vs missing
- [x] Integrate lookup through `ResolveSample(...)`
- [x] Add physical sample-resolution tests
- [x] Manually confirm a stable/lazer behavior difference for explicit custom filenames

Completed compatibility work:

- [x] Preserve the explicit-filename comparison beatmap as a reproducible fixture
- [x] Record each test object's exact `hitSound` flags and `hitSample.filename`
- [x] Use documented legacy/osu!stable custom-only semantics as the canonical behavior for explicit `hitSample.filename`
- [x] Update logical hitobject custom-filename behavior
- [x] Add regression tests for the chosen custom-only compatibility semantics
- [x] Preserve a second fixture covering spinner and remaining slider behavior
- [x] Verify that slider trailing `HitSample.Index`, `HitSample.Volume`, and `HitSample.Filename` do not override edge/body/tick sample generation
- [x] Resolve slider edge `SampleIndex` / `Volume` from the sample timing point at the edge time
- [x] Resolve slider body/tick `SampleIndex` / `Volume` from the sample timing point at slider start
- [x] Cover pre-first-timing-point and no-timing-point sample fallbacks
- [x] Reach `182/182` passing tests

Current next compatibility question:

- [ ] Verify slider `edgeSets = 0:0` inheritance semantics from official evidence or a reproducible fixture before changing code

Remaining Physical Sample Resolution work:

- [ ] Review whether any additional physical fallback states are required
- [ ] Define stable logical `SampleId` only when identity requirements are clear
- [ ] Load only the sample metadata required by later editor/audio work
- [ ] Run the full Physical Sample Resolution regression suite and review the boundary before moving to Audio Infrastructure
Do not introduce global optimization yet.

---

## Audio Infrastructure

**Status: NOT STARTED**

Prefer an independently evaluated open-source .NET audio library for infrastructure rather than implementing low-level playback or decoding ourselves.

Planned:

- [ ] Evaluate/adopt audio library
- [ ] Decode required audio formats
- [ ] Basic playback
- [ ] Seek
- [ ] Play hitsound layers together
- [ ] Synchronize playback position with editor timeline

---

## Desktop Editor and Timeline

**Status: NOT STARTED**

Primary UI technology: WPF unless a materially better reason emerges.

Planned:

- [ ] Waveform
- [ ] Hitsound events/tracks
- [ ] Selection
- [ ] Editing
- [ ] Snapping
- [ ] Reusable patterns if still beneficial after core editing works

External libraries may be used for generic UI/audio infrastructure after evaluation.

---

## Export and Round-Trip Verification

**Status: NOT STARTED**

This stage must work deterministically before global optimization.

Planned:

- [ ] Export `.osu`
- [ ] Export required sample files
- [ ] Re-read exported beatmap
- [ ] Reconstruct logical hitsound representation
- [ ] Compare source and exported logical representations
- [ ] Verify samples, multiplicity, timing, volume, and overlaps
- [ ] Add integration tests

Required invariant:

```text
parse
-> logical representation
-> export
-> reload
-> logical equivalence
```

---
## osu!tools Submission Milestone

**Target: after Export and Round-Trip Verification**

At this point, evaluate whether the project is ready to be submitted to the osu!tools directory.

Before submission:

- [ ] The editor provides a complete useful workflow for another user.
- [ ] A beatmap can be opened, edited, and exported.
- [ ] Hitsounds can be previewed reliably.
- [ ] Exported beatmaps pass round-trip logical equivalence verification.
- [ ] A downloadable GitHub release is available.
- [ ] Basic user documentation exists.
- [ ] Installation and first-use instructions have been tested from a clean environment.
- [ ] Known important limitations are documented.
- [ ] Re-check the current osu!tools submission requirements.
- [ ] Prepare screenshots, description, download link, and repository link as required.

Sample Optimization is not required for the first submission unless later development shows that it is necessary for the editor's core workflow.

---

## Sample Optimization

**Status: NOT STARTED**

Only begin after deterministic round-trip equivalence exists.

Planned objective:

1. Minimize redundant physical sample files.
2. Among equally minimal solutions, minimize effective `(SampleSet, SampleIndex)` combinations.
3. Preserve sonic/logical equivalence.

Possible future approach:

- deterministic heuristic first
- global optimizer / CP-SAT only if the problem demonstrably requires it

Do not assume perceptual similarity means sample equivalence.

Sample equivalence must be based on an explicit rule, such as identical audio content or an equivalence declared by the system or user.

---

## Integration and Release Polish

**Status: NOT STARTED**

Planned:

- [ ] Error handling
- [ ] Performance profiling
- [ ] Larger beatmap testing
- [ ] Usability pass
- [ ] Packaging
- [ ] Documentation
- [ ] Release checklist
