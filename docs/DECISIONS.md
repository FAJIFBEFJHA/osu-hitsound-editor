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

## D016 — Slider audio responsibilities remain separate until a concrete consumer requires aggregation

**Decision:** Keep slider edge hitsounds, continuous slider body hitsounds, and slider tick hitsounds as separate resolution responsibilities for now.

Do not introduce a combined API such as `GetAllSliderHitSounds()` until a concrete consumer, such as the timeline, requires a unified event representation with appropriate timing information.

**Reason:** These slider sounds have different timing and behavior. Introducing a combined abstraction before its required data shape is known would be premature.

---

## D017 — Physical sample resolution preserves osu! lookup semantics

**Decision:** Physical sample resolution must preserve osu!'s lookup semantics instead of resolving a sample from its generated filename alone.

`SampleIndex = 0` and `SampleIndex = 1` may correspond to the same base filename, but they do not have the same lookup behavior.

The resolver must preserve whether the beatmap directory participates in the lookup and distinguish between:

- a sample found in the beatmap
- an external fallback
- an explicit custom filename that was found
- an explicit custom filename that is missing

**Reason:** Reducing physical resolution to filename generation would lose information required to reproduce osu!'s actual sample lookup behavior.