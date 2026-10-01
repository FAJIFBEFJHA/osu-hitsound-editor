# AI_WORKFLOW

Use this file as instructions for any AI assistant helping with `osu-hitsound-editor`.

## Language

- Explain concepts in Spanish.
- Use English for class names, method names, properties, variables, exception messages, Git commits, and other technical identifiers.

## Primary learning rule

- The user writes production/project code.
- Do NOT provide complete implementations for code under `src/` unless the user explicitly asks for the complete implementation.
- Complete code MAY be provided for:
  - tests
  - Git commands
  - configuration
  - diagnostics
  - temporary scripts/tools
  - exception-handling expressions and exception constructors/messages

## New methods

When introducing a new method, always use exactly this structure:

```text
SECCIÓN:
    Where the method belongs in the current class/file.

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

- Keep `FLUJO` concise and directly translatable to code.
- Do NOT expand it into a long numbered walkthrough unless the user asks.
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
- use one concrete example

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

## Project-specific rules

- Prefer VS Code while sufficient.
- Say when Visual Studio becomes materially advantageous or necessary.
- Parser, osu! timing logic, logical hitsound representation, exporter, and optimizer are primarily our own implementation.
- External open-source libraries are acceptable for infrastructure such as audio/UI after independent evaluation.
- For uncertain osu! format behavior, verify official osu! documentation or the official open-source implementation instead of guessing.
- Keep logical sample identity separate from physical `.osu` `SampleSet` / `SampleIndex` / filename representation.
- Do not introduce CP-SAT or another global optimizer until deterministic parse -> logical representation -> export -> reload -> equivalence verification works.

## Continuity

Treat:

- `docs/PROJECT_CONTEXT.md` as the current project checkpoint.
- `docs/DECISIONS.md` as established architectural decisions.
- `docs/ROADMAP.md` as the planned sequence.
- tests and actual source code as the ultimate implementation truth.

If chat context conflicts with those sources, point out the discrepancy instead of guessing.

## Current checkpoint

At the time this file was created:

```text
Phase: 2 — Real hitsound resolution
Tests: 79/79 passing
```

Read `docs/PROJECT_CONTEXT.md` for the current checkpoint because this number will become stale.
