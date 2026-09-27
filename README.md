# osu-hitsound-editor

A personal learning project for building a desktop hitsound editing tool for osu! beatmaps using C# and .NET.

The project is being developed incrementally as a way to learn practical software development while exploring a DAW-like workflow for osu! hitsounding.

> [!NOTE]
> This project is currently in early development. The core `.osu` parsing and hitsound resolution logic is being built before the desktop interface.

## Goals

The long-term goal is to provide a DAW-like workflow for osu! hitsounding, including:

- Visual timeline editing
- Audio waveform visualization
- Hitsound tracks
- Beat snapping
- Reusable hitsound patterns
- Sample preview and playback
- Importing existing `.osu` beatmaps
- Efficient sample organization
- Optimized `.osu` export
- Reduction of unnecessary duplicated sample files

One of the main design goals is to separate the sound the mapper wants to use from the way osu! represents that sound through `SampleSet`, `SampleIndex`, `HitSound`, and sample filenames.

## Current Status

The project currently focuses on parsing and understanding osu!standard beatmap data.

Implemented:

- `.osu` metadata parsing
- General beatmap information parsing
- Timing point parsing
- BPM calculation
- Hit object parsing
- Hit circle detection
- Slider detection
- Spinner detection
- New combo detection
- Hitsound bit flag interpretation
  - Normal
  - Whistle
  - Finish
  - Clap
- Hit sample parsing
  - NormalSet
  - AdditionSet
  - SampleIndex
  - Volume
  - Filename
- Spinner end time parsing
- Active timing point resolution
- Hitsound inheritance resolution
  - SampleIndex
  - Volume
  - NormalSet
  - AdditionSet
- Sample set representation
  - Default
  - Normal
  - Soft
  - Drum
- Automated tests with xUnit

## Project Structure

```text
osu-hitsound-editor/
├── src/
│   └── OsuHitsoundEditor/
│       ├── Beatmap.cs
│       ├── BeatmapLoader.cs
│       ├── HitObject.cs
│       ├── HitSample.cs
│       ├── HitSoundType.cs
│       ├── SampleSetType.cs
│       ├── TimingPoint.cs
│       └── OsuHitsoundEditor.csproj
│
├── tests/
│   └── OsuHitsoundEditor.Tests/
│       ├── BeatmapTests.cs
│       ├── BeatmapLoaderTests.cs
│       └── TestData/
│           └── basic-beatmap.osu
│
├── docs/
│   └── architecture.md
│
└── OsuHitsoundEditor.sln
```

See [Architecture](docs/architecture.md) for diagrams and more information about the current design.

## Requirements

- .NET 10 SDK

Check your installed version with:

```bash
dotnet --version
```

## Build

Clone the repository and run:

```bash
dotnet build
```

## Tests

Run the complete test suite with:

```bash
dotnet test
```

The test suite currently covers the core beatmap parsing and hitsound inheritance logic.

## Development Roadmap

### Core beatmap model

- [x] Metadata parsing
- [x] Timing point parsing
- [x] Basic hit object parsing
- [x] HitSample parsing
- [x] HitSound flag interpretation
- [x] Sample inheritance resolution
- [x] SampleSet type resolution
- [x] Automated tests

### Hitsound resolution

- [ ] Resolve logical hitsound layers
- [ ] Resolve actual sample files
- [ ] Complete slider edge hitsounds
- [ ] Support slider head, repeat and tail samples
- [ ] Handle sample filenames and sample indexes consistently

### Audio and editor

- [ ] Audio playback
- [ ] Hitsound preview
- [ ] Waveform visualization
- [ ] Timeline
- [ ] Beat snapping
- [ ] Hitsound tracks
- [ ] Reusable patterns

### Export

- [ ] Modify and export `.osu` files
- [ ] Preserve existing beatmap information
- [ ] Automatically assign sample indexes
- [ ] Detect duplicate audio samples
- [ ] Reuse compatible samples
- [ ] Reduce unnecessary duplicated sample files
- [ ] Validate final beatmap sample size

## Sample Optimization

osu! hitsounds can require the same audio sample to be duplicated under multiple sample indexes when it is used in different combinations.

For example:

```text
drum-hitnormal2.wav
drum-hitnormal3.wav
drum-hitnormal4.wav
```

may all contain the exact same kick sample.

A future `SampleOptimizer` will analyze the logical samples used throughout the beatmap and attempt to minimize this duplication while maintaining compatibility with osu!'s normal hitsound system.

## Architecture

The project currently separates beatmap parsing, beatmap data, hit objects, timing points, and hitsound information.

Future versions are expected to introduce components such as:

```text
SampleResolver
AudioPlayer
Timeline
SampleOptimizer
Exporter
```

These components are documented in more detail in [`docs/architecture.md`](docs/architecture.md).

## Disclaimer

## Learning Project and AI Assistance

This repository is primarily a personal learning project created to practice C#, .NET, software architecture, testing, Git, and desktop application development.

ChatGPT was used throughout the development process as a programming mentor and learning assistant.

The workflow was intentionally structured so that the project code was not generated wholesale. Instead, ChatGPT was mainly used to:

- Explain C# and .NET concepts.
- Discuss software design decisions.
- Suggest architecture and project organization.
- Explain how the osu! file format and hitsound system work.
- Provide pseudocode before implementation.
- Review code written by the project author.
- Help identify bugs and implementation mistakes.
- Assist with Git, testing, documentation, and development tooling.
- Provide complete code for tests, temporary diagnostics, and non-project tooling when useful.

The main project implementation has been written incrementally by the project author as part of the learning process.

Because this is a practice project, some parts of the architecture and implementation may change significantly as new concepts are learned and the application evolves.