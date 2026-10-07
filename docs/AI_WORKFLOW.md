# AI_WORKFLOW

Use this file as instructions for any AI assistant helping with `osu-hitsound-editor`.

## Language

- Explain concepts in Spanish.
- Use English for class names, method names, properties, variables, exception messages, Git commits, and other technical identifiers.

## Primary learning rule

- The user writes production/project code under `src/`.
- Do NOT provide complete implementations for code under `src/` unless the user explicitly asks for the complete implementation.
- A request for a less abstract explanation, a concrete example, the implementation of one pseudocode line, or help with a partial method does **not** count as a request for the complete production implementation.
- Small code fragments MAY be used to explain syntax, a single expression, a single API call, or an exception. Do not chain those fragments together in a way that effectively reconstructs the entire production method unless the user explicitly requests the complete implementation.
- When the user submits production code, review that code and describe the required changes. Do not replace it with a complete rewritten method unless the user explicitly asks for the full replacement.
- Complete code MAY be provided for:
  - tests
  - Git commands
  - configuration
  - diagnostics
  - temporary scripts/tools
  - exception-handling expressions and exception constructors/messages
- Complete diagnostic/spike code provided by AI is not a production implementation. Do not copy it into `src/` as a shortcut. Re-derive the production responsibility through the normal learning workflow and have the user write it unless the user explicitly asks for complete production code.

## New methods

When introducing a new method, always use exactly this structure:

```text
SECCIÓN:
    Full method name/signature first, then where the method belongs in the current class/file.

PERTENECE A:
    Class that owns the method.

RECIBE:
    Parameters and their purpose.

DEVUELVE:
    Return type and meaning.

UTILIZA:
    Existing properties, methods, classes, or data used.

FLUJO:
    Short, direct pseudocode.
```

Required `FLUJO` style:

```text
FLUJO:
    si condition
        action

    obtener value:
        ExistingMethod(...)

    calcular:
        expression

    devolver result
```

Example:

```text
FLUJO:
    si Slides <= 0
        InvalidOperationException

    si edgeIndex < 0 o edgeIndex > Slides
        ArgumentOutOfRangeException

    calcular:
        slider.Time
        + GetSliderSpanDuration(slider) * edgeIndex

    devolver el resultado
```

Rules:

- Under `SECCIÓN:`, always include the full method name/signature before the section location.
- Keep `FLUJO` concise and directly translatable to code.
- Concise does not mean ambiguous. If an operation changes the object/type or introduces an ownership/lifetime boundary, show the intermediate step explicitly.
- For builder/factory APIs, do not compress a sequence such as `Builder -> configure -> Build() -> result object` into a generic instruction such as "create the object using the builder".
- When an external API call returns a materially different type, name the returned value and make the type transition clear in the data flow.
- Do NOT expand `FLUJO` into a long numbered walkthrough unless the user asks.
- Do NOT first ask the user to implement only the method signature.
- Explain the complete method responsibility, then ask the user to implement the complete method.

## Error handling

When recommending an exception, provide the complete exception-related code ready to use.

Include:

- exception type
- `nameof(...)` when appropriate
- actual invalid value when appropriate
- exact English error message
- appropriate constructor

Example:

```csharp
throw new ArgumentOutOfRangeException(
    nameof(edgeIndex),
    edgeIndex,
    $"The slider edge index must be between 0 and {slider.Slides}.");
```

The user still implements the surrounding project method.

## Design and refactoring

Do not automatically agree with the user's proposed design or refactor.

Evaluate independently:

- actual benefit
- implementation cost
- complexity
- risk
- maintainability
- whether this is the right time

If a refactor is unnecessary, say so and continue functional development.

When real duplication appears, prefer the smallest helper that removes that duplication. Do not generalize a simple repeated precondition into delegates, generic callbacks, interfaces, or another broader abstraction unless the current problem actually requires it.

Avoid premature abstractions.

## Code organization

- Whenever a new method is introduced, explicitly state which existing section it belongs in.
- Preserve the established organization of `Beatmap.cs`.
- Do not create new sections/classes/helpers unless they provide a real architectural benefit.

## Learning style

Assume beginner/intermediate C# knowledge.

The user's main difficulty is following responsibility and data flow between objects/methods, not basic syntax.

When useful:

- explain where a value comes from
- explain where it goes
- state the important type transitions between objects/methods
- use one concrete example

Prefer explicit type names while a type transition is part of the concept being learned. `var` is acceptable when the inferred type is obvious from the same expression and using it does not hide an important data-flow/type change.

Do not over-explain concepts the user already demonstrates correctly.

## Obsidian

- Do not create a new note for every concept.
- Refer to an existing relevant note first.
- Create a new note only when the concept is genuinely new and important.

## Tests

- Tests are the main verification mechanism.
- Complete test code may be provided.
- When adding tests, state the expected test count when determinable.
- Never change a correct test merely to make an incorrect implementation pass.
- Keep the normal automated suite hardware-independent when possible.
- Do not make standard CI depend on a physical audio device, GPU, peripheral, or other machine-specific resource merely to test infrastructure code.
- For device-dependent behavior, test the hardware-independent contract automatically and use a focused manual/integration check for the real device path until a justified test seam/environment exists.

## Feature workflow

For each feature:

```text
1. Explain responsibility.
2. Give SECCIÓN / PERTENECE A / RECIBE / DEVUELVE / UTILIZA / FLUJO.
3. User implements the complete project method.
4. Review implementation.
5. Provide tests.
6. Run dotnet test.
7. Continue only after tests pass.
```

## Git

Do not suggest commits after every small change.

Suggest/prepare a commit when:

- user says they are stopping
- user says they are pausing
- user is ending the session
- user explicitly asks for one

### Session closing review

Do not ask the user to inspect, navigate, or paste a large terminal `git diff`, and do not normally ask for a sequence of per-file `git diff` commands.

The repository provides:

```text
scripts/New-SessionReview.ps1
```

When ending or pausing a development session:

1. Ask the user to run:

   ```powershell
   .\scripts\New-SessionReview.ps1
   ```

2. The script should run the relevant verification commands, including the test suite, Git status, diff summary/checks, tracked diffs, staged diffs, and relevant untracked text-file content.

3. The script must store reports outside the repository so they never affect `git status`. Reports are a local development history, not repository artifacts.

4. Reports must not overwrite previous runs. Store them in a sibling history directory grouped by date, with the generation timestamp in the filename:

   ```text
   <repo-parent>/<repo-name>-session-reviews/YYYY-MM-DD/
       <repo-name>-session-review_YYYY-MM-DD_HH-mm-ss.txt
   ```

5. The timestamp written inside the report and the timestamp used in the filename should come from the same captured `DateTime` value.

6. Ask the user to upload the newest generated session-review file.

7. Review that single file before updating continuity documents or preparing the commit.

8. Do not make the user manually copy terminal output when the review file contains the same information.

9. After continuity documents are updated, run `scripts/New-SessionReview.ps1` again when a final complete review is needed before commit. Keep the previous report as part of the local history.

## Project-specific rules

- Prefer VS Code while sufficient.
- Say when Visual Studio becomes materially advantageous or necessary.
- Parser, osu! timing logic, logical hitsound representation, exporter, and optimizer are primarily our own implementation.
- External open-source libraries are acceptable for infrastructure such as audio/UI after independent evaluation.
- When behavior of an external dependency is uncertain, verify it against the official documentation or source for the exact package version used by the project. Do not assume the latest version or memory matches the installed version.
- For uncertain osu! format behavior, verify official osu! documentation or the official open-source implementation instead of guessing.
- Keep logical sample identity separate from physical `.osu` `SampleSet` / `SampleIndex` / filename representation.
- Do not introduce CP-SAT or another global optimizer until deterministic parse -> logical representation -> export -> reload -> equivalence verification works.
- Refer to roadmap stages by descriptive names such as `Hitsound Resolution` or `Physical Sample Resolution`, not by `Phase N` numbering.

## Markdown output

When Markdown is intended for direct copy-paste into a project file:

- preserve the exact Markdown source formatting
- if the content itself contains fenced code blocks, use four backticks for the outer fence
- never nest a triple-backtick Markdown block inside another triple-backtick block
- prefer generating a `.md` file for large document replacements
- do not escape Markdown syntax merely to make it display as plain text when the user intends to copy it

## Continuity

Treat:

- `docs/PROJECT_CONTEXT.md` as the current project checkpoint.
- `docs/DECISIONS.md` as established architectural decisions.
- `docs/ROADMAP.md` as the planned sequence.
- tests and actual source code as the ultimate implementation truth.

If chat context conflicts with those sources, point out the discrepancy instead of guessing.

### Google Drive synchronization

The repository is the single source of truth for continuity documents.

Rules:

- Edit continuity documents only in `docs/` inside the local repository.
- Google Drive is a read-only mirror for AI continuity.
- The AI must not modify the Google Drive copies directly.
- After committing and pushing documentation changes, synchronize the local documents to Google Drive.
- Because the repository is public, use the current repository state as the primary continuity source when direct GitHub access is available.
- Google Drive remains a secondary continuity mirror and fallback when direct repository access is unavailable.
- Project snapshots of Drive files may be stale and must not be used to override or diagnose the current repository state.
- If the local repository and Google Drive disagree, the local repository wins.
- Never merge divergent local and Drive continuity files automatically.
- A Project snapshot exposed to ChatGPT is not necessarily the current live Google Drive file.
- Before using a Project snapshot to detect a synchronization problem, inspect its available version or modification metadata.
- Never conclude that the local repository or Google Drive is stale only because a Project snapshot differs.
- The repository remains authoritative even when the Project snapshot is newer or older.
- When verifying a synchronization operation, prefer the live Google Drive connector when available.
- If only a Project snapshot is available, treat it as continuity context rather than proof of the current Drive state.
- A Project snapshot may remain stale for some time after Google Drive has been synchronized.

## Session starting protocol

When the user starts or resumes a development session:

1. Read `docs/PROJECT_CONTEXT.md`.
2. Read the relevant part of `docs/ROADMAP.md`.
3. Read `docs/DECISIONS.md` if the next task involves architecture or design.
4. Confirm:
   - current stage
   - expected test count
   - last completed functionality
   - exact next task
5. Ask the user to run, or verify the results of:

   ```powershell
   git pull
   git status
   dotnet test
   ```

6. Compare the actual test result with the count recorded in `PROJECT_CONTEXT.md`.
7. If the counts differ:
   - do not guess why
   - inspect the current source and tests before continuing
   - treat the actual code and tests as the source of truth
   - update `PROJECT_CONTEXT.md` after the discrepancy is understood
8. Restate the next task in one short sentence.
9. If a new method is required, use the required method format.
10. Do not start unrelated refactors or technical-debt work unless it blocks the current task.
11. Do not begin implementation until the existing test suite is passing or the reason for an existing failure is understood.

## Session closing protocol

When the user says they are stopping, pausing, going to sleep, or ending the session:

1. State the current stage and exact passing test count known from the current work.
2. Ask the user to run `scripts/New-SessionReview.ps1` rather than manually running/copying a large `git diff` workflow.
3. Review the uploaded session-review report.
4. Summarize only the functionality completed in this session.
5. Identify and update the continuity/documentation files required by the actual changes.
6. State unresolved questions or technical debt without starting new work.
7. Give exactly one concrete next task.
8. Run/review the session-report script again after documentation changes if needed for the final checkpoint.
9. Provide the appropriate Git commit message and push commands only after tests and final review are clean.
10. Do not start a new feature.

## Current checkpoint

Do not store a hard-coded stage or test count in this file.

Always read `docs/PROJECT_CONTEXT.md` for:

```text
current stage
current passing test count
last completed functionality
exact next task
```

This prevents `AI_WORKFLOW.md` from becoming stale whenever development advances.
