# osu-hitsound-editor

> A desktop hitsound editor for osu!standard with a DAW-like workflow, built as a C#/.NET learning project.

---

## Overview

`osu-hitsound-editor` is a tool designed to make hitsound placement faster and more visual. It is intended for mappers who finish creating a beatmap only to realize that the hitsounding process can be repetitive and time-consuming.

The goal is to provide an editor with a workflow closer to a DAW such as FL Studio, making it easier to organize, preview, and work with hitsounds without dealing directly with every low-level detail of the `.osu` format.

## Why this project exists

This project exists for two main reasons.

1. Mapping is fun, and hitsounds can add a lot to a beatmap, but placing and managing them manually can take a considerable amount of time.

2. I wanted to learn programming through an actual project. Instead of moving from tutorial to tutorial, I decided to build something I would genuinely like to use and learn C#/.NET along the way, with ChatGPT acting as a mentor when I need guidance.

## Current Status

The project is still in an early stage of development and is not yet a complete end-user tool. The beatmap parsing, hitsound-resolution, and physical sample-resolution layers are already in place, and the current focus is the audio infrastructure.

Basic audio decoding and WASAPI playback foundations are implemented. The remaining work in this stage includes reliable timeline playback, seeking, and keeping the playback position synchronized with the editor timeline. The desktop editor, export workflow, and sample optimization are still planned for later stages.

> [!NOTE]
> This project is still under active development. The core parsing, hitsound-resolution, physical sample-resolution, and early audio infrastructure layers already exist, but the desktop editing interface is not implemented yet.

## Features

### Implemented

- Parse and interpret `.osu` beatmap files
- Handle hit object, slider, and timing information
- Resolve logical hitsound layers
- Resolve physical sample files inside the beatmap
- Decode MP3, WAV, and OGG audio
- Provide the foundation for audio playback through WASAPI

### Planned

- Visual timeline editor
- Hitsound sample preview
- Song waveform visualization
- Beat snapping and timeline editing
- Beatmap export
- Reduction of unnecessary duplicate hitsound sample files

## How it works

The process begins by parsing the `.osu` file into an internal beatmap model. From that model, the program determines which hitsound layers should play for each relevant event before resolving them to physical audio files.

This keeps the logical meaning of a hitsound separate from the way osu! stores it through `SampleSet`, `SampleIndex`, and filenames. That separation makes it possible to work with hitsounds at a higher level first and deal with the physical `.osu` representation only when necessary.

```text
.osu file
    ↓
BeatmapLoader
    ↓
parsed beatmap model
    ↓
logical hitsound resolution
    ↓
physical sample resolution
    ↓
audio infrastructure
    ↓
editor / export
```

See [`docs/architecture.md`](docs/architecture.md) for the detailed architecture and data flow.

## Screenshots / Preview

> [!NOTE]
> The desktop editor UI has not been implemented yet, so there is no visual editor preview at this stage.

## Technology

- C#
- .NET 10
- NAudio 3.1.0
- NAudio.Vorbis 3.0.0
- xUnit
- Windows / WASAPI
- WPF planned for the desktop UI

## Requirements

- Windows
- .NET 10 SDK
- Git

Check the installed .NET version with:

```powershell
dotnet --version
```

## Building from Source

```powershell
git clone https://github.com/FAJIFBEFJHA/osu-hitsound-editor.git
cd osu-hitsound-editor
dotnet build
```

## Tests

Run the complete automated suite with:

```powershell
dotnet test
```

The regression suite covers areas such as:

- `.osu` parsing
- timing and slider behavior
- hitsound inheritance and logical layer resolution
- physical sample resolution
- hardware-independent audio behavior
- relevant invalid-state and boundary cases

## Project Structure

```text
osu-hitsound-editor/
├── src/        # Production code
├── tests/      # Automated tests and fixtures
└── docs/       # Architecture, roadmap and decisions
```

## Roadmap

- [x] Beatmap Parsing and Base Model
- [x] Hitsound Resolution
- [x] Physical Sample Resolution at the current boundary
- [ ] Audio Infrastructure
- [ ] Desktop Editor and Timeline
- [ ] Export and Round-Trip Verification
- [ ] Sample Optimization
- [ ] Integration and Release Polish

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the detailed development plan.

## Documentation

- [`docs/architecture.md`](docs/architecture.md) — current architecture and data flow
- [`docs/ROADMAP.md`](docs/ROADMAP.md) — development stages and planned work
- [`docs/DECISIONS.md`](docs/DECISIONS.md) — established architectural and behavioral decisions
- [`CONTRIBUTING.md`](CONTRIBUTING.md) — contribution guidelines

## Contributing

Contributions, bug reports, suggestions, and documentation improvements are welcome.

This is also a learning project, so changes should remain understandable, testable, and consistent with the current architecture.

See [`CONTRIBUTING.md`](CONTRIBUTING.md) before working on larger changes.

## AI-assisted Development

This project is also a practical way for me to learn C#/.NET, with substantial help from ChatGPT along the way.

The AI is used primarily as a mentor rather than as the main author of the production code. For code under `src/`, I normally write the implementation myself. The AI can explain a method's responsibility, help trace data flow, provide pseudocode, review my implementation, and assist with debugging or research. Complete production implementations are only provided when I explicitly ask for them. Tests, diagnostics, temporary tools, configuration, and workflow automation may be generated more directly when that is useful.

The point of documenting this is not to hide how much AI assistance the project receives. I would rather make that clear from the beginning than pretend the project was developed without it.

The development workflow, collaboration rules, project checkpoint system, session-review process, and reusable workflow template are maintained in a separate repository:

[AI-Assisted Development Workflow](https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow)

The project-specific configuration used for `osu-hitsound-editor` can be found here:

[`projects/osu-hitsound-editor/`](https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/tree/main/projects/osu-hitsound-editor)

The source code and tests in this repository remain the implementation truth. The workflow documentation exists to make the development process easier to resume and reproduce across sessions and devices; it does not replace source control, testing, or the project's technical documentation.

## License

This project is licensed under the MIT License.

See [`LICENSE`](LICENSE) for details.

## Disclaimer

This is an independent personal learning project.

It is not an official osu! project and is not affiliated with ppy. osu!, its trademarks, formats, and third-party assets remain the property of their respective owners.

