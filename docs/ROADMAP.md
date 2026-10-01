# ROADMAP

## Goal

Build a Windows desktop hitsound editor for osu!standard with a DAW-like workflow while using the project to learn C#, .NET, architecture, testing, Git, and desktop application development.

The roadmap should remain incremental. Avoid large architectural jumps before the previous layer works deterministically.

---

## Phase 1 — Parser and base model

**Status: COMPLETE**

Implemented foundations include:

- `.osu` section parsing
- General metadata
- Editor settings
- Difficulty settings
- Timing points
- Hit objects
- Hit samples
- sliders
- slider edges
- spinners
- hitsound flags
- base test suite

---

## Phase 2 — Real hitsound resolution

**Status: IN PROGRESS**

Current checkpoint: **79/79 tests passing**

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
- [x] Resolve slider edge hitsounds
- [x] Calculate slider span duration
- [x] Calculate total slider duration
- [x] Calculate real slider edge times
- [x] Resolve slider edge samples using timing at the edge's real time
- [x] Resolve continuous slider body `SliderSlide`
- [x] Resolve continuous slider body `SliderWhistle`

Remaining / review:

- [ ] Determine the clean API for resolving all slider edge hitsounds
- [ ] Verify remaining slider-specific audio behavior needed by the editor
- [ ] Review slider ticks / continuous slider audio requirements before deciding whether they belong in Phase 2 or a later phase
- [ ] Add missing edge cases discovered during final Phase 2 review
- [ ] Run full Phase 2 regression suite
- [ ] Complete publication-readiness review
- [ ] Review slider tick behavior
---

## Public repository milestone

**Target: beginning of Phase 3**

Before making the repository public:

- [ ] All tests pass
- [ ] Remove personal/local-only paths
- [ ] Remove secrets
- [ ] Remove copyrighted beatmaps/audio that should not be redistributed
- [ ] Verify `.gitignore`
- [ ] Fix known naming/typo issues
- [ ] Review README in the author's own voice
- [ ] Keep the honest AI-mentorship disclosure
- [ ] Add license
- [ ] Review `docs/architecture.md`
- [ ] Review `docs/DECISIONS.md`
- [ ] Review `docs/PROJECT_CONTEXT.md`

---

## Phase 3 — Physical sample files and sample resolution

**Status: NOT STARTED**

Planned:

- [ ] Discover sample files used by the beatmap
- [ ] Resolve osu! sample filenames
- [ ] Separate logical sample identity from physical `.osu` representation
- [ ] Define stable logical `SampleId`
- [ ] Load sample metadata required by the editor
- [ ] Handle missing samples predictably
- [ ] Add sample-resolution tests

Do not introduce global optimization yet.

---

## Phase 4 — Audio infrastructure

**Status: NOT STARTED**

Prefer an independently evaluated open-source .NET audio library for infrastructure rather than implementing low-level playback/decoding ourselves.

Planned:

- [ ] Evaluate/adopt audio library
- [ ] Decode required audio formats
- [ ] Basic playback
- [ ] Seek
- [ ] Play hitsound layers together
- [ ] Synchronize playback position with editor timeline
- [ ] Handle audio device errors

---

## Phase 5 — Desktop UI and timeline

**Status: NOT STARTED**

Primary UI technology: WPF unless a materially better reason emerges.

Planned:

- [ ] WPF application shell
- [ ] Open beatmap
- [ ] Timeline
- [ ] Playhead
- [ ] Zoom
- [ ] waveform
- [ ] hitsound events/tracks
- [ ] selection
- [ ] editing
- [ ] snapping
- [ ] reusable patterns if still beneficial after core editing works

External libraries may be used for generic UI/audio infrastructure after evaluation.

---

## Phase 6 — Exporter and round-trip verification

**Status: NOT STARTED**

This phase must work deterministically before global optimization.

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

## Phase 7 — Sample optimization

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

---

## Phase 8 — Integration and release-quality polish

**Status: NOT STARTED**

Planned:

- [ ] Error handling
- [ ] performance profiling
- [ ] larger beatmap testing
- [ ] usability pass
- [ ] packaging
- [ ] documentation
- [ ] release checklist
