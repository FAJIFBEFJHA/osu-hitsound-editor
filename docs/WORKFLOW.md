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

Choose one small functional objective and define how the session will end.

Use:

```text
OBJECTIVE:
    one concrete functional goal

DONE WHEN:
    observable completion conditions

VERIFICATION:
    automated and/or manual evidence required

OUT OF SCOPE:
    nearby work that must not expand the current task
```

Avoid combining:

```text
new feature
+ large refactor
+ architecture rewrite
```

in one step.

---

## Before implementing a new method

First define the behavior contract:

```text
NORMAL:
    what observable result should happen

INVALID / BOUNDARY:
    relevant states that must be rejected or handled

VERIFICATION:
    automated regression test and/or focused manual/integration check
```

If the method creates or retains a resource such as an `IDisposable`, `Stream`, `WaveStream`, file handle, audio device, or event subscription, also answer:

```text
Who creates it?
Who owns it?
How long must it live?
Who releases it?
What happens if initialization fails partway through?
```

Then define the method using:

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
    When relevant, classify implementation data as:
        FIELD NUEVO
        LOCAL NUEVA
        PARAMETER
        EXISTENTE
    State its exact type, declaration location, scope when relevant, and responsibility.

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

Before implementation, verify that the proposed flow covers the applicable:

```text
normal path
error paths
variable scope
ownership / lifetime
cleanup
state transitions
threading / concurrency
```

Avoid ambiguous assignment arrows when source and destination could be confused. Prefer `destination = source` or explicitly say "store source in destination".

Do not introduce a predictable requirement from this review only after the author has already implemented the method, unless new evidence genuinely appeared later.

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

If a real-device verification is written as a temporary integration test, remove that test after the behavior is verified and rerun the permanent automated suite.

Use the evidence type deliberately:

```text
automated regression test
    -> deterministic project behavior

manual/integration verification
    -> hardware, OS integration, or external environment

diagnostic spike
    -> temporary API/behavior research
    -> not production code
```

Decide the required evidence before implementation rather than after the code is already written.

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

Use this only as a signal:

```text
first occurrence
    -> normally keep local

second similar occurrence
    -> observe

third identical/simple repetition
    -> explicitly evaluate a small helper
```

This is not a mathematical rule. A real responsibility boundary can justify a helper earlier, while superficially similar code can remain separate when its semantics differ.

Prefer functional progress over premature abstraction.

---

## Automation threshold

Do not automate a workflow merely because automation is possible.

Use the manual process first long enough to understand:

```text
the steps
failure modes
actual repetition/friction
human-error risk
```

Automate only when that experience demonstrates a concrete benefit, unless automation is required for correctness, reproducibility, or safety.

---

## Documentation updates and patches

For partial changes to an existing tracked text/documentation file, prefer a Git patch over replacing the entire file.

```text
existing tracked file + partial changes
    -> .patch

new file
    -> complete file

deliberate full replacement
    -> complete replacement file
```

The patch must be generated against the version the user actually has locally. If the target file has uncommitted local changes, do not use a stale repository or Project/Drive snapshot as the base.

Apply patches in two steps:

```powershell
git apply --check .\change.patch
git apply .\change.patch
```

If the check fails:

```text
stop
do not force
identify the base-version mismatch
regenerate/reconcile the patch
```

---

## Verification proportional to risk

Do not repeat expensive verification mechanically when the latest change cannot affect behavior.

```text
production / tests / executable script logic changed
    -> run relevant verification
    -> run dotnet test when project behavior may be affected

meaningful documentation/workflow checkpoint
    -> inspect the actual diff
    -> include it in the normal session review

formatting-only correction after an already reviewed report
    -> git diff --check
    -> git status --short
    -> no automatic full report/test rerun
```

If a change touches executable semantics, dependency versions, generated artifacts, or configuration behavior, it is not formatting-only.

---

## Working across multiple devices

Use Git/GitHub, not a synchronized folder, as the repository transport.

```text
DEVICE A
    -> verify current work
    -> commit
    -> push
    -> confirm clean working tree

DEVICE B
    -> git pull
    -> git status
    -> dotnet test
    -> continue
```

Rules:

- Each device has its own clone.
- Do not place the Git repository itself inside Google Drive/OneDrive as a synchronization strategy.
- Do not copy the repository folder between devices to transfer changes.
- Avoid modifying the same checkpoint on two devices at once.
- Prefer a stable checkpoint on `main` before switching devices.
- If unfinished work is not appropriate for `main`, push it on a temporary branch and continue that branch on the other device.
- `git stash` is local and is not a cross-device transfer mechanism.
- Never force-push `main`.
- Unexpected local changes or conflicts must be inspected before destructive Git commands are used.

### Portable scripts and local configuration

Tracked scripts must not contain:

```text
drive letters
user-profile paths
absolute repository paths
device-specific Google Drive mount paths
machine-specific secrets
```

Use repository-relative discovery through Git or `$PSScriptRoot`.

External machine-specific locations belong in environment variables or another explicitly local/untracked configuration source.

Routine repository scripts target Windows PowerShell 5.1 compatibility unless the task genuinely requires a newer runtime.

Before introducing a newer PowerShell/.NET API:

```text
Is the newer API materially required?
    no
        -> use a Windows PowerShell 5.1-compatible alternative

    yes
        -> document the required PowerShell version
        -> fail clearly when the required version is unavailable
```

The Google Drive continuity mirror uses:

```text
OSU_HITSOUND_CONTEXT_MIRROR
```

Configure that variable separately on each device. The repository stores only the variable name, never the actual local path.

Generic setup form:

```powershell
[Environment]::SetEnvironmentVariable(
    "OSU_HITSOUND_CONTEXT_MIRROR",
    "<local-mirror-directory>",
    "User")
```

Open a new PowerShell session after changing a user-level environment variable.

The tracked synchronization command is always:

```powershell
.\scripts\Sync-ProjectContext.ps1
```

`scripts/New-SessionReview.ps1` and `scripts/Sync-ProjectContext.ps1` must derive their working locations dynamically. Session review content should use portable placeholders such as `<repo-root>` rather than embedding the current device's absolute repository path.

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

After meaningful documentation or executable changes, run the review script again when a final complete review is needed:

```powershell
.\scripts\New-SessionReview.ps1
```

If the only post-review change was formatting-only, use the proportional lightweight checks instead of repeating the full report automatically.

If the current functional/documentation checkpoint is complete and the report is clean:

```powershell
git add ...
git commit -m "..."
git push
```

Google Drive is an optional continuity mirror, not the primary project checkpoint.

When direct repository access is available, use the pushed repository state for continuity and verification.

If the Drive mirror is used, synchronize it only after repository changes have been committed and pushed:

```powershell
.\scripts\Sync-ProjectContext.ps1
```

The script reads `OSU_HITSOUND_CONTEXT_MIRROR` from the current device. No device-specific path belongs in the tracked script or documentation.

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
