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

A combined `GetAllSliderHitSounds()`-style API remains intentionally deferred until a concrete consumer requires it.

The Hitsound Resolution baseline was `100/100` tests before Physical Sample Resolution began.

---

## Public Repository Milestone

**Status: COMPLETE**

- [x] Publication-readiness review
- [x] Review/remove local-only paths and secrets
- [x] Review redistributed content
- [x] Verify `.gitignore`
- [x] Review README and AI-mentorship disclosure
- [x] Add license
- [x] Publish repository

Repository:

```text
https://github.com/FAJIFBEFJHA/osu-hitsound-editor
```

---

## Physical Sample Resolution

**Status: COMPLETE AT CURRENT BOUNDARY**

Latest Physical Sample Resolution checkpoint before Audio Infrastructure: **190/190 tests passing**

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
- [x] Preserve reproducible explicit-filename compatibility fixtures
- [x] Use legacy/osu!stable custom-only semantics as canonical explicit-filename behavior
- [x] Verify slider trailing `HitSample.Index`, `HitSample.Volume`, and `HitSample.Filename` do not override edge/body/tick generation
- [x] Resolve slider edge `SampleIndex` / `Volume` from the sample timing point at the edge time
- [x] Resolve slider body/tick `SampleIndex` / `Volume` from the sample timing point at slider start
- [x] Cover pre-first-timing-point and no-timing-point sample fallbacks
- [x] Verify and implement slider `edgeSets = 0:0` inheritance from the sample timing point rather than slider trailing sample-bank values
- [x] Add pre-first-timing-point edge-set regression coverage
- [x] Add universal bankless beatmap sample fallback after the specific banked candidate
- [x] Ensure `SampleIndex = 0` does not use universal beatmap samples
- [x] Keep universal beatmap hits under `BeatmapSampleFound` rather than adding another resolution outcome
- [x] Run the full regression suite before moving into Audio Infrastructure

Deliberately deferred until a concrete consumer requires them:

- [ ] Define stable logical `SampleId`
- [ ] Load additional sample metadata
- [ ] Refactor duplicated standard/body/tick lookup construction

These deferred items do not block Audio Infrastructure and should not be introduced prematurely.

---

## Audio Infrastructure

**Status: IN PROGRESS**

Current full-suite checkpoint: **210/210 tests passing**

Selected infrastructure:

- [x] Evaluate/adopt NAudio 3.1.0
- [x] Add NAudio.Vorbis 3.0.0 for OGG decoding
- [x] Target production/tests to `net10.0-windows`
- [x] Validate `WasapiPlayer` as the modern Windows playback direction

Diagnostic spike completed and removed after validating:

- [x] WAV/MP3/OGG decoding
- [x] basic play/pause/resume/stop
- [x] seek during playback
- [x] simultaneous sample mixing
- [x] sample-rate normalization
- [x] mono-to-stereo normalization
- [x] multichannel-to-stereo behavior using the first two channels
- [x] per-layer volume
- [x] low-latency WASAPI behavior
- [x] rendered output clock as timeline basis
- [x] seek timeline-base model

Production implementation completed:

- [x] `AudioPlaybackEngine.OpenAudioFile(...)`
- [x] WAV/MP3 production decoding through `AudioFileReader`
- [x] OGG production decoding through `VorbisWaveReader`
- [x] missing-file and unsupported-format behavior
- [x] `AudioPlaybackEngine.NormalizeForMixer(...)`
- [x] convert decoded input to `ISampleProvider`
- [x] normalize mono to stereo
- [x] normalize >2 channels by preserving channels 0 and 1
- [x] resample to a caller-provided target sample rate
- [x] automated production tests for opening and normalization
- [x] keep `AudioPlaybackEngine` construction hardware-independent
- [x] explicitly initialize production output with `WasapiPlayerBuilder`
- [x] build a persistent stereo float `MixingSampleProvider`
- [x] use `DeviceMixFormat.SampleRate` as the mixer/output target sample rate
- [x] initialize `WasapiPlayer` with the persistent mixer
- [x] add guarded `Play()` / `Pause()` / `Stop()` controls
- [x] centralize initialized-output validation in `GetInitializedOutputDevice()`
- [x] implement `IDisposable` cleanup for the owned output device
- [x] add hardware-independent control-state tests
- [x] Add one decoded/normalized audio source to the persistent mixer
- [x] Define and implement source-reader ownership/disposal for mixer inputs
- [x] Verify real production playback with a focused manual/device check
- [x] Add a persistent timeline-audio source boundary with `LoadTimelineAudio(...)`
- [x] Keep the timeline decoder alive after its mixer input ends so future seek can reposition it
- [x] Add the hardware-independent `LoadTimelineAudio(...)` uninitialized-output contract test
- [x] Verify timeline-reader lifetime through focused real-device integration checks
- [x] Implement `Seek(double timeMilliseconds)` with fresh normalized providers and rendered-clock reset
- [x] Implement `GetTimelinePositionMilliseconds()` using the timeline base and WASAPI rendered position
- [x] Make `Stop()` return the loaded timeline to the beginning
- [x] Apply per-source volume (0-100) using `VolumeSampleProvider` in `AddAudioSource(...)`
- [x] Verify simultaneous mixing of two sources with individual volumes
- [x] Guard partial `InitializeOutput()` failures with device disposal and event unsubscription
- [x] Verify the successful output-initialization path through a focused device test

Next:

- [ ] Connect one resolved `HitSoundLayer` to `SampleResolver.ResolveSample(...)` and `AudioPlaybackEngine.AddAudioSource(...)`, passing its resolved volume
- [ ] Define playback behavior for missing custom samples and external fallback requirements
- [ ] Add dynamic simultaneous hitsound triggering at beatmap event times
- [ ] Connect the rendered-position timeline clock to the eventual editor timeline/UI
- [ ] Handle device errors and runtime recovery beyond partial-initialization cleanup

Do not copy the removed diagnostic spike into `src/`. Production methods are re-derived and written by the author through the normal learning workflow.

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

Before submission:

- [ ] The editor provides a complete useful workflow for another user
- [ ] A beatmap can be opened, edited, and exported
- [ ] Hitsounds can be previewed reliably
- [ ] Exported beatmaps pass round-trip logical equivalence verification
- [ ] A downloadable GitHub release is available
- [ ] Basic user documentation exists
- [ ] Installation and first-use instructions have been tested from a clean environment
- [ ] Known important limitations are documented
- [ ] Re-check the current osu!tools submission requirements
- [ ] Prepare screenshots, description, download link, and repository link as required

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
