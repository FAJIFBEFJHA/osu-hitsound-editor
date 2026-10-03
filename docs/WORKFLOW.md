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

---

## While implementing

Keep track of data flow:

```text
Where does this value come from?
What type is it?
Which method transforms it?
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

Prefer functional progress over premature abstraction.

---

## Before ending a session

First verify the working state:

```powershell
dotnet test
git status
git diff
```

Then update `docs/PROJECT_CONTEXT.md` so it records the checkpoint that is actually about to be committed:

```text
CURRENT STAGE
TEST STATUS
COMPLETED
CURRENT STATE
NEXT TASK
IMPORTANT / TECHNICAL DEBT
```

Use descriptive roadmap stage names such as:

```text
Hitsound Resolution
Physical Sample Resolution
Export and Round-Trip Verification
```

Do not use `Phase N` numbering for roadmap stages.

`PROJECT_CONTEXT.md` is a checkpoint, not a diary. Keep it current and focused on information needed to resume development.

Review the final changes again:

```powershell
git status
git diff
```

If the current functional or documentation checkpoint is complete:

```powershell
git add ...
git commit -m "..."
git push
```

If a local continuity mirror is used, synchronize it only after the repository changes have been committed and pushed.

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

If an experiment becomes messy:

```powershell
git status
git diff
dotnet test
```

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
