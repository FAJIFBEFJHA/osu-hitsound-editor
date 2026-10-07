# DECISIONS

This file records architectural decisions that should not be casually reopened in every session.

If a decision becomes wrong because of new evidence, update this file deliberately and document why.

---

## D001 — Project code is written by the author

**Decision:** The author writes production/project code under `src/`.

AI may:

- explain responsibilities and data flow
- provide pseudocode
- review author-written code
- provide small syntax/API fragments that do not amount to a complete production method
- provide complete tests
- provide Git commands
- provide configuration
- provide diagnostics
- provide temporary scripts/tools
- provide complete exception-handling expressions/messages

A request for a less abstract explanation, a concrete syntax example, implementation of one pseudocode line, or help with a partial method is not permission to provide the complete surrounding production implementation.

When small code fragments are used for teaching, they must not be chained together in a way that effectively reconstructs the full production method unless the author explicitly requests the complete implementation.

Complete code created by AI for a diagnostic spike, temporary tool, or experiment is evidence used to learn/validate behavior. It must not be copied into `src/` as production implementation merely because it already exists. Production behavior is re-derived through the normal learning workflow and written by the author unless the author explicitly asks for a complete production implementation.

**Reason:** The project is also a C#/.NET learning project. The author needs to follow responsibility, type changes, data flow, and ownership rather than merely receive working production code. Allowing either diagnostic code or a sequence of "small" snippets to bypass the production-code rule would defeat that learning goal.

---

## D002 — Logical sample identity is independent from `.osu` physical representation

**Decision:** A logical sample must not be identified only by `SampleSet`, `SampleIndex`, or filename.

**Reason:** The optimizer needs to reason about what should sound independently from how osu! encodes the sample physically.

---

## D003 — Sample equivalence is explicit, not perceptual

**Decision:** Samples are considered equivalent only by a defined criterion, such as identical audio content or an explicit equivalence declared by the system/user.

**Reason:** Similar-sounding samples are not necessarily interchangeable.

---

## D004 — Optimization objective is hierarchical

**Decision:** Optimization must preserve logical and sonic equivalence.

Among valid equivalent representations, the intended optimization priority is:

1. Minimize redundant physical sample files.
2. Among equally minimal solutions, minimize effective `(SampleSet, SampleIndex)` combinations.

**Reason:** Correct behavior is a hard constraint, not something that may be traded away for a smaller representation. File duplication is the primary optimization target.

---

## D005 — No global optimizer yet

**Decision:** Do not introduce CP-SAT or another global optimizer until this pipeline works deterministically:

```text
parse
-> logical representation
-> export
-> reload
-> equivalence verification
```

**Reason:** Optimization without a trustworthy equivalence verifier would make correctness difficult to establish.

---

## D006 — Parser, osu! timing logic, logical hitsound model, exporter and optimizer are primarily our implementation

**Decision:** Keep the core domain logic inside this project.

**Reason:** These areas are central to the learning goal and to the project's unique functionality.

---

## D007 — External open-source libraries are acceptable for infrastructure

**Decision:** Libraries may be used for infrastructure such as:

- audio decoding/playback
- generic UI controls
- other non-core platform functionality

Before adding a dependency, evaluate:

- license
- maintenance status
- .NET compatibility
- transitive dependencies
- benefit
- replacement cost
- architectural intrusion
- whether it removes an important learning/core-domain task

---

## D008 — VS Code remains the primary IDE

**Decision:** Continue using VS Code while it remains sufficient.

**Reason:** Current project needs do not require Visual Studio.

Revisit only if Visual Studio provides a material development advantage.

---

## D009 — Tests are the primary verification mechanism

**Decision:** Functional work proceeds in small steps and tests must pass before moving to the next behavior.

**Reason:** Tests provide a stable checkpoint and reduce reliance on AI/chat memory.

---

## D010 — Beatmap.cs should remain organized without premature abstractions

**Decision:** Preserve clear method sections in `Beatmap.cs`. Do not introduce interfaces, partial classes, resolver classes, or other abstractions only to make the file look cleaner.

**Reason:** Split responsibilities only when a real architectural boundary appears.

---

## D011 — Slider timing uses double precision where required

**Decision:** Methods that query arbitrary timing instants use `double time` where slider edge or tick timing can be fractional.

**Reason:** Slider repeat, tail, and tick times may not be integer milliseconds.

---

## D012 — Invalid state should not silently become valid-looking data

**Decision:** Do not use values such as `0` to silently represent an impossible calculation when the method contract expects a valid result.

Example:

`GetSliderSpanDuration` throws when no active uninherited timing point exists rather than returning `0`.

---

## D013 — Public repository starts before Physical Sample Resolution

**Decision:** Make the repository public after **Hitsound Resolution** is complete and the publication-readiness review passes, before beginning **Physical Sample Resolution**.

**Reason:** Hitsound Resolution establishes a coherent parser and logical-resolution foundation suitable for public development.

---

## D014 — README should reflect the author's voice

**Decision:** Before publication, rewrite/review README so it sounds like the author's own documentation rather than generic AI-generated prose.

Keep an honest disclosure that AI was used as a mentor/learning assistant.

---

## D015 — Official sources for uncertain osu! behavior

**Decision:** When osu! format behavior is uncertain, verify it using:

1. official osu! documentation
2. official open-source osu! implementation when necessary

Do not guess format behavior.

---

## D016 — Slider hitsound responsibilities remain separate

**Decision:** Keep slider edge, slider body, and slider tick hitsound resolution as separate responsibilities and APIs for now.

Do not add a broad `GetAllSliderHitSounds()` method unless a concrete consumer requires it.

**Reason:** Edge, body, and tick sounds have different timing and sample-resolution rules. Combining them early would hide those distinctions without providing a current architectural benefit.

---

## D017 — Physical sample resolution preserves osu! lookup semantics

**Decision:** Physical sample resolution must preserve the semantic distinction between `SampleIndex = 0` and `SampleIndex = 1` and must not represent all missing local files as the same state.

The physical outcome model distinguishes:

```text
BeatmapSampleFound
ExternalFallbackRequired
CustomSampleFound
CustomSampleMissing
```

**Reason:** A standard sample that is absent from the beatmap may legitimately continue to skin/default fallback, while a missing explicitly named custom sample is a different condition. Likewise, index `0` must not accidentally become an index `1` beatmap lookup.

---

## D018 — Physical filesystem lookup belongs to SampleResolver

**Decision:** Keep filesystem/sample-name resolution in `SampleResolver` rather than adding it to `Beatmap`.

`Beatmap` remains responsible for logical/timing resolution. `SampleResolver` translates a resolved `HitSoundLayer` into osu! physical lookup behavior.

**Reason:** Physical resource discovery is now a concrete responsibility boundary. Keeping it separate prevents `Beatmap` from mixing timing/logical semantics with filesystem concerns.

---

## D019 — Explicit custom filename behavior is client-sensitive

**Status:** Superseded by D020 after the compatibility target was deliberately chosen.

**Decision:** Do not change logical custom-filename semantics to match only osu!stable or only osu!lazer until the project explicitly chooses a compatibility target.

Manual testing confirmed a behavioral divergence: stable explicit-filename cases behaved as custom-only in the current comparison, while lazer allowed the explicit custom sample to coexist with applicable addition samples.

Before changing `GetHitSoundLayers(...)`, preserve or recreate a reproducible fixture and record each input case explicitly.

**Reason:** Choosing one behavior implicitly could make parse -> logical representation -> export -> reload equivalence incorrect for the other client. The compatibility target must be deliberate and testable.

---

## D020 — Legacy explicit custom filename semantics are canonical

**Decision:** For legacy `.osu` hit objects with an explicit `hitSample.filename`, the editor's canonical logical audible result is the explicitly named custom sample only.

The parsed source data remains intact: the original `hitSound` flags and `hitSample.filename` are preserved. Suppression of `Whistle`, `Finish`, and `Clap` additions belongs to logical hitsound resolution, not parsing.

The current osu!lazer behavior, where an explicit custom sample can coexist with applicable additions, is treated as client-specific behavior rather than the canonical legacy model.

Do not introduce a stable/lazer compatibility mode unless a concrete consumer later requires client-specific playback or export behavior.

**Reason:** The documented legacy format semantics and manual osu!stable testing agree on custom-only playback for explicit filenames, while manual osu!lazer testing confirms a real client divergence. Choosing the legacy/stable semantics gives the project one deterministic logical model for parse -> logical representation -> export -> reload -> equivalence without prematurely introducing client-target abstractions.

---

## D021 — Legacy slider trailing hitSample fields have limited scope

**Decision:** For legacy `.osu` sliders, preserve the trailing `hitSample` source fields but do not treat all of them as ordinary slider-wide overrides.

Current canonical behavior:

```text
HitSample.NormalSet
HitSample.AdditionSet
    -> participate in slider body/tick sample-bank resolution

HitSample.Index
HitSample.Volume
HitSample.Filename
    -> do not override slider edge/body/tick logical sample generation
```

For slider edges:

```text
explicit SliderEdge.NormalSet / AdditionSet
    -> edge-specific sample-bank values

edge NormalSet = 0
    -> inherit from the sample timing point at the edge time

edge AdditionSet = 0
    -> inherit the resolved effective NormalSet

SampleIndex / Volume
    -> sample timing point at the edge's real time
```

For slider body and ticks:

```text
sample sets
    -> slider-level normal/addition sample-bank values as applicable

SampleIndex / Volume
    -> sample timing point at slider start
```

When a required sample timing value is queried before the first timing point, use the first timing point in the map. If the beatmap contains no timing points, use the documented/default legacy values `SampleIndex = 0` and `Volume = 100` where applicable.

**Reason:** Official legacy behavior treats an explicit edge bank value of `0` as unspecified and resolves it through the sample control point at the edge time. Slider trailing sample-bank fields remain relevant to body/tick resolution but do not override explicit edge `0:0` inheritance.

---

## D022 — Universal bankless beatmap samples are a physical fallback for SampleIndex >= 1

**Decision:** For standard, slider-body, and slider-tick sample lookup with `SampleIndex >= 1`, resolve physical beatmap candidates in this order:

```text
specific banked beatmap sample
-> universal bankless beatmap sample
-> external/user-skin fallback
```

Examples of universal bankless beatmap names include:

```text
hitnormal.wav
hitwhistle.wav
hitfinish.wav
hitclap.wav
sliderslide.wav
sliderwhistle.wav
slidertick.wav
```

For `SampleIndex = 0`, neither the banked nor universal beatmap candidate is used.

A universal beatmap sample still produces `BeatmapSampleFound`; it does not require a new resolution outcome.

**Reason:** This preserves the legacy lookup semantics verified against the official implementation while keeping physical resolution outcomes focused on where the sample was found rather than which beatmap filename tier matched.

---

## D023 — NAudio is the selected Windows audio infrastructure

**Decision:** Use:

```text
NAudio 3.1.0
NAudio.Vorbis 3.0.0
```

for Windows audio infrastructure.

Production and test projects target:

```text
net10.0-windows
```

Use `WasapiPlayer` rather than obsolete `WasapiOut` for production WASAPI playback.

Do not add `NAudio.SoundFile` / libsndfile unless a concrete requirement appears.

**Reason:** NAudio provides the required Windows playback, mixing, sample-provider, decoder, and WASAPI functionality without replacing the project's core osu! domain work. The diagnostic spike validated the required formats and playback behavior on the target platform.

---

## D024 — Playback inputs normalize to stereo and the device mix sample rate

**Decision:** The production audio pipeline uses float `ISampleProvider` data and normalizes playback inputs as follows:

```text
mono
    -> duplicate to stereo

stereo
    -> preserve

more than 2 channels
    -> preserve input channels 0 and 1
    -> discard additional channels

sample rate
    -> resample to the active output device mix sample rate
```

Do not hard-code `44100 Hz` as the production mixer/output sample rate.

**Reason:** The low-latency WASAPI spike showed that IAudioClient3 low-latency mode requires the source rate to match the device engine mix rate. The target device in the spike used `48000 Hz`, and resampling to that rate allowed low-latency mode to activate. Preserving the first two channels also matches the compatibility behavior investigated for osu!'s BASS-based mixer path more closely than averaging all channels.

---

## D025 — Decoder position is not the editor playback clock

**Decision:** Do not use decoder `CurrentTime` as the authoritative editor playhead while audio is playing.

For WASAPI playback, the timeline model is based on rendered output position:

```text
timeline position
    = timeline base after the most recent seek
    + rendered device position
```

A seek resets/restarts the output clock and updates the timeline base to the requested target position.

**Reason:** The diagnostic spike showed that decoder position runs ahead of audible/rendered playback because of buffering, while `WasapiPlayer.GetPosition()` tracks frames rendered by the device. After pause drains the existing padding, source/rendered positions converge; after seek, a timeline base plus rendered position reconstructs the intended editor time.
---

## D026 — Audio output initialization is explicit and constructor-independent

**Decision:** `AudioPlaybackEngine` construction must remain independent from opening a physical audio device.

The output lifecycle is explicit:

```text
new AudioPlaybackEngine()
    -> no WASAPI device opened

InitializeOutput()
    -> create/configure WasapiPlayer
    -> read DeviceMixFormat.SampleRate
    -> create persistent stereo float mixer
    -> Init(mixer)

Play() / Pause() / Stop()
    -> require initialized output

Dispose()
    -> release owned output device
    -> clear output/mixer references
```

`outputDevice` and `mixer` are persistent fields because later playback operations must use the same initialized output/mixer instance.

Ordinary automated tests should remain hardware-independent where practical. Device-dependent WASAPI behavior is verified with focused manual/integration checks until a justified hardware test seam/environment exists.

**Reason:** Existing decoder/normalization tests instantiate `AudioPlaybackEngine` without needing a physical audio endpoint, and GitHub Actions should not become dependent on local playback hardware merely because output support exists. Explicit initialization also makes the resource ownership and lifecycle boundary visible.
