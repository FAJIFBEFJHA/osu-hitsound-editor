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

Current checkpoint: **100/100 tests passing**

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

The current slider responsibilities remain separate because head/repeat/tail events, continuous body sounds, and ticks have different timing and behavior. A combined event API should only be introduced when the timeline or another concrete consumer requires it.

---

## Public Repository Milestone

**Target: before starting Physical Sample Resolution**

Completed:

- [x] All tests pass
- [x] Verify `.gitignore`
- [x] Verify no tracked local Windows paths
- [x] Check tracked files for obvious secrets or credentials
- [x] Verify test data does not include redistributable copyrighted beatmap/audio content
- [x] Fix known naming/typo issues
- [x] Review README in the author's own voice
- [x] Keep the honest AI-mentorship disclosure
- [x] Add `CONTRIBUTING.md`
- [x] Add MIT `LICENSE`
- [x] Review `docs/architecture.md`
- [x] Review this roadmap

Remaining:

- [ ] Review `docs/DECISIONS.md`
- [ ] Review `docs/PROJECT_CONTEXT.md`
- [ ] Review `docs/AI_WORKFLOW.md` and `docs/WORKFLOW.md` for stale numbered-stage references
- [ ] Replace remaining `Phase N` terminology in project documentation where it refers to roadmap stages
- [ ] Run final `dotnet test`
- [ ] Review final `git status` and `git diff`
- [ ] Sync the final local project-context documents to the read-only Drive mirror
- [ ] Make the publication commit and push when the documentation review is complete

---

## Physical Sample Resolution

**Status: NOT STARTED**

This is the next functional development stage after the public-repository milestone is complete.

Planned:

- [ ] Discover sample files used by the beatmap
- [ ] Resolve osu! sample filenames
- [ ] Separate logical sample identity from physical `.osu` representation
- [ ] Define a stable logical `SampleId`
- [ ] Load sample metadata required by the editor
- [ ] Handle missing samples predictably
- [ ] Add sample-resolution tests

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
- [ ] Handle audio-device errors

---

## Desktop Editor and Timeline

**Status: NOT STARTED**

Primary UI technology: WPF unless a materially better reason emerges.

Planned:

- [ ] WPF application shell
- [ ] Open beatmap
- [ ] Timeline
- [ ] Playhead
- [ ] Zoom
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
