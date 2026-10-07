# WORKFLOW

This is the default workflow for developing `osu-hitsound-editor`, with or without AI assistance.

---

## Start of a session

Run:

```powershell
git pull
git status
dotnet test
```

Do not start new functional work if the existing test suite is unexpectedly failing.

Then read:

1. `docs/PROJECT_CONTEXT.md`
2. `docs/ROADMAP.md`
3. `docs/DECISIONS.md` when the task involves architecture/design
4. `docs/architecture.md` when the task crosses architectural boundaries

Choose one small functional objective.

Avoid combining:

```text
new feature
+ large refactor
+ architecture rewrite
```

in one step.

---

## Before implementing a new method

Define it using:

```text
SECCIÓN:
    Full method name/signature, then its existing section in the class/file.

PERTENECE A:
    Class that owns the method.

RECIBE:
    Parameters and their purpose.

DEVUELVE:
    Return type and meaning.

UTILIZA:
    Existing properties, methods, classes, or data used.

FLUJO:
    Short pseudocode directly translatable to code.
```

Always include the full method name/signature under `SECCIÓN:` before the section location.

`FLUJO` must be short and directly translatable to code.

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

Then implement the complete method.

Do not split the normal process into:

```text
write signature
-> review
-> write body
```

unless the signature itself is the concept being studied.

Diagnostic or temporary-tool code may be complete, but it must not be copied into `src/` as a shortcut around the learning workflow.

When working with an AI mentor:

```text
production code under src/
    -> author writes it

less abstract explanation
single pseudocode line
syntax example
partial-method review
    -> not permission for a complete production implementation
```

Small snippets are acceptable for explaining one expression, one API call, or one exception, but they must not be assembled into a de facto complete production method unless the author explicitly asks for the full implementation.

`FLUJO` must expose important intermediate transformations. In particular:

```text
builder
    -> configure builder
    -> Build()
    -> concrete result object
```

must not be compressed into an ambiguous instruction such as "create the result using the builder" when the intermediate type change matters.

---

## While implementing

Keep track of data flow:

```text
Where does this value come from?
What type is it?
Which method transforms it?
Does that call return a different type/object?
Who owns the result and how long must it live?
Where does the result go?
```

If confused, use a concrete trace:

```text
variable                  value
--------------------------------
slider.Time               1500
spanDuration              500
edgeIndex                 1
edgeTime                  2000
```

When using an external library and the API behavior is uncertain, verify the official documentation/source for the exact package version referenced by the project before building further instructions on top of it.

When a type transition itself is part of what is being learned, prefer an explicit type in the example. `var` is fine when the inferred type is obvious on the same line and does not hide an important transformation.

---

## Error handling

Do not hide impossible states with plausible-looking return values.

Use appropriate exceptions.

When using `ArgumentOutOfRangeException`, prefer constructors that include:

- `nameof(parameter)`
- actual invalid value
- exact English message

Example:

```csharp
throw new ArgumentOutOfRangeException(
    nameof(edgeIndex),
    edgeIndex,
    $"The slider edge index must be between 0 and {slider.Slides}.");
```

Exception messages and technical identifiers are written in English.

---

## Tests

After implementing behavior:

1. Add tests for the normal case.
2. Add relevant boundary/error cases.
3. Run:

```powershell
dotnet test
```

Do not change a correct test merely to make an incorrect implementation pass.

Keep the standard automated suite independent of physical hardware when possible. If a behavior requires a real audio device or another machine-specific resource:

```text
hardware-independent contract
    -> automated test

real device path
    -> focused manual/integration verification
```

Do not make ordinary CI depend on hardware unless the project deliberately introduces an appropriate integration-test environment.

If tests fail:

```text
What did I expect?
What actually happened?
Which variable first became incorrect?
Where did that variable get its value?
Which method produced the first unexpected value?
```

Use the stack trace before changing code.

---

## Refactoring rule

Do not refactor merely because the code could look cleaner.

Ask:

```text
Is there a real problem now?
    no -> do not refactor
    yes
        -> does the refactor help the current task,
           remove real duplication,
           reduce risk,
           or clarify an actual responsibility boundary?
```

Evaluate:

- benefit
- implementation cost
- complexity
- risk
- maintainability
- timing

When duplication is real, use the smallest helper that removes it. Do not turn a simple repeated precondition into a generic delegate/callback/interface abstraction unless the current feature actually benefits from that extra flexibility.

Prefer functional progress over premature abstraction.

---

## Before ending a session

Do not use a large interactive terminal `git diff` as the normal closing-review workflow.

Run the repository review script:

```powershell
.\scripts\New-SessionReview.ps1
```

The script should create one report outside the repository containing the information needed for review, including:

```text
dotnet test result
git status --short
git diff --stat
git diff --check
unstaged tracked diff
staged diff
relevant untracked text-file contents
```

Keep session reports as local history rather than overwriting one file. Store them outside the repository using this structure:

```text
<repo-parent>/<repo-name>-session-reviews/YYYY-MM-DD/
    <repo-name>-session-review_YYYY-MM-DD_HH-mm-ss.txt
```

The timestamp inside the report and the timestamp in its filename should be generated from the same captured time value. Do not add this report-history directory to the repository.

If working with an AI mentor, upload the newest generated report instead of manually copying terminal output or navigating multiple per-file diffs.

Review the report before updating the continuity checkpoint.

Then update `docs/PROJECT_CONTEXT.md` so it records the checkpoint that is actually about to be committed:

```text
CURRENT STAGE
TEST STATUS
COMPLETED
CURRENT STATE
NEXT TASK
IMPORTANT / TECHNICAL DEBT
```

Update `docs/ROADMAP.md` when stage/progress changed.

Update `docs/DECISIONS.md` only when an architectural/behavioral decision became stable enough that it should not be reopened every session.

Update `AI_WORKFLOW.md` / `WORKFLOW.md` only when the development process itself changed.

Use descriptive roadmap stage names such as:

```text
Hitsound Resolution
Physical Sample Resolution
Audio Infrastructure
Export and Round-Trip Verification
```

Do not use `Phase N` numbering for roadmap stages.

`PROJECT_CONTEXT.md` is a checkpoint, not a diary. Keep it current and focused on information needed to resume development.

After documentation changes, run the review script again when a final complete review is needed:

```powershell
.\scripts\New-SessionReview.ps1
```

If the current functional/documentation checkpoint is complete and the report is clean:

```powershell
git add ...
git commit -m "..."
git push
```

Google Drive is an optional continuity mirror, not the primary project checkpoint.

When direct repository access is available, use the pushed repository state for continuity and verification.

If the Drive mirror is used, synchronize it only after repository changes have been committed and pushed.

When verifying the continuity mirror:

```text
repository / pushed main
    -> authoritative checkpoint

live Google Drive
    -> mirror to verify

Project snapshot exposed to AI
    -> potentially stale cached/indexed copy
```

Do not diagnose a synchronization failure from a Project snapshot alone.
If freshness matters, verify the live mirror directly or compare its metadata first.

---

## Recovery when stuck

Reduce the problem.

Instead of:

```text
How do I implement slider timing?
```

ask:

```text
What value do I need next?
What data produces that value?
Can I test that transformation independently?
```

If an experiment becomes messy, use the smallest useful checks first:

```powershell
git status --short
git diff --stat
git diff --check
dotnet test
```

If a complete diff review is required, generate the session-review file instead of navigating a large terminal diff.

Return to the last known-good checkpoint when necessary.

---

## Source-of-truth hierarchy

Use this order:

```text
1. tests and actual source code
2. official osu! documentation / official implementation
3. docs/DECISIONS.md
4. docs/PROJECT_CONTEXT.md
5. docs/ROADMAP.md
6. AI/chat advice
```

An AI suggestion never overrides verified project behavior silently.
