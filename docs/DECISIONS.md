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
- provide complete tests
- provide Git commands
- provide configuration
- provide diagnostics
- provide temporary tools/scripts
- provide complete exception-handling expressions/messages

**Reason:** The project is also a C#/.NET learning project.

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

Before changing `GetHitSoundLayers(...)`, preserve or recreate a reproducible explicit-filename fixture and record its input cases explicitly.

**Reason:** The documented legacy format semantics and manual osu!stable testing agree on custom-only playback for explicit filenames, while manual osu!lazer testing confirms a real client divergence. Choosing the legacy/stable semantics gives the project one deterministic logical model for parse -> logical representation -> export -> reload -> equivalence without prematurely introducing client-target abstractions.
