# Contributing

Contributions, bug reports, suggestions, and documentation improvements are welcome.

This is also a learning project, so the goal is not only to make the code work, but to keep the implementation understandable, testable, and consistent with the current architecture.

## Before contributing

For small fixes, documentation changes, or clearly isolated improvements, feel free to open a pull request directly.

For larger features, architectural changes, major refactors, or changes that affect the project roadmap, please open an issue first so the idea can be discussed before a large amount of work is done.

## Development setup

Requirements:

- .NET 10 SDK
- Git
- A C# editor or IDE

VS Code is currently the primary development environment used by the project, but contributors are free to use another editor or IDE.

Clone the repository and build normally:

```bash
dotnet build
```

Run the complete test suite with:

```bash
dotnet test
```

## Tests

Tests are the main verification mechanism used by this project.

Before submitting a pull request:

- Run the full test suite.
- Make sure all existing tests still pass.
- Add tests for new behavior when appropriate.
- Add relevant boundary and error cases when they matter.
- Do not change a correct test just to make an incorrect implementation pass.

If a change intentionally modifies existing behavior, explain why the old behavior is no longer correct.

## osu! behavior

Do not guess uncertain osu! format or hitsound behavior.

When behavior is unclear, verify it using:

1. Official osu! documentation.
2. The official osu! open-source implementation when documentation is not enough.

If a pull request depends on a non-obvious osu! behavior, including a reference to the relevant official source is appreciated.

## Project architecture

The project is intentionally developed incrementally.

Please avoid introducing new abstractions, interfaces, helper classes, architectural layers, or optimization systems only because they may be useful in the future.

A new abstraction should solve a real problem that already exists.

Relevant project documentation:

- [`docs/DECISIONS.md`](docs/DECISIONS.md)
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/ROADMAP.md`](docs/ROADMAP.md)
- [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md)

When documentation and implementation disagree, the current source code and tests should be treated as the implementation truth.

## Core project logic

The following areas are considered core parts of the project:

- `.osu` parsing
- osu! timing behavior
- logical hitsound representation
- exporting
- sample optimization

These areas are intentionally implemented inside this project rather than delegated entirely to external libraries.

External libraries are acceptable for infrastructure such as:

- audio playback or decoding
- generic UI components
- platform integration

Before introducing a dependency, consider:

- license
- maintenance status
- .NET compatibility
- transitive dependencies
- long-term replacement cost
- whether it replaces an important part of the project's learning goal

## Logical and physical sample representation

A logical sample should not be treated as identical to its physical osu! representation.

In particular:

```text
SampleSet
SampleIndex
Filename
```

are part of how osu! stores and resolves a sample, but they are not automatically the identity of the sound itself.

Changes in this area should preserve that separation.

## Refactoring

Refactors are welcome when they solve an actual problem.

Please avoid refactors whose only purpose is making the code look more abstract or "cleaner."

A useful refactor should do at least one of the following:

- remove meaningful duplication
- clarify an existing responsibility boundary
- reduce implementation risk
- improve maintainability of code that is already becoming difficult to work with
- make the current feature easier to implement or verify

Large unrelated refactors should not be mixed into feature pull requests.

## Error handling

Invalid states should not silently produce valid-looking results.

If a method cannot produce a meaningful result because required data is invalid or missing, prefer an appropriate exception over returning a misleading fallback value.

Exception messages and technical identifiers should be written in English.

## Test data and copyrighted content

Do not submit copyrighted beatmaps, songs, sample packs, or other assets that cannot legally be redistributed.

For automated tests, prefer:

- synthetic `.osu` files
- minimal test fixtures
- generated or freely redistributable data

Do not include personal beatmap collections or commercial audio files.

## Local and sensitive files

Do not commit:

- build output
- `bin/`
- `obj/`
- local machine paths
- personal scripts
- secrets
- API keys
- tokens
- credentials
- editor-specific temporary files

Check `.gitignore` before adding new generated or local-only files.

## AI-assisted contributions

AI-assisted contributions are allowed.

Contributors are expected to understand and take responsibility for the code they submit.

Using AI to help with:

- explanations
- debugging
- tests
- research
- documentation
- design discussion
- code review
- implementation assistance

is acceptable.

However, please do not submit large amounts of generated code that you cannot explain, verify, or maintain.

AI-assisted code is expected to meet the same testing, architecture, readability, and correctness requirements as any other contribution.

## Pull requests

A pull request should ideally:

- have a clear purpose
- avoid unrelated changes
- pass the full test suite
- include tests for new behavior when appropriate
- explain important design decisions
- mention any osu!-specific behavior that required external verification
- update documentation if the change makes existing documentation inaccurate

Small pull requests are generally easier to review than large ones.

## Commit messages

Use concise commit messages that describe the actual change.

Examples:

```text
Add slider tick timing resolution
Fix inherited sample set handling
Add tests for invalid slider multiplier
Update hitsound resolution documentation
```

Avoid vague messages such as:

```text
fix stuff
changes
update
final version
```

## Code style

Try to follow the existing style of the project.

In particular:

- Use English for code identifiers.
- Prefer clear names over short names.
- Keep responsibilities easy to follow.
- Preserve the existing organization of files unless there is a good reason to change it.
- Avoid premature abstractions.

Consistency with the surrounding code is more important than introducing a completely different style in one pull request.

## Questions and suggestions

If you are unsure whether a change fits the current direction of the project, open an issue first.

Suggestions are welcome, including suggestions that challenge existing design decisions, as long as the reasoning is explained.