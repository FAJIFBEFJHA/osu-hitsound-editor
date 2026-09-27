# Architecture

This document describes the current architecture of **osu-hitsound-editor**.

Its purpose is to provide a visual reference for how the classes are related and how data flows from an `.osu` file into the application's internal representation.

> The architecture will continue to evolve as the project grows.

---

## Current Class Structure

```mermaid
classDiagram

    class Program {
        +Main()
    }

    class BeatmapLoader {
        +Load(path) Beatmap
        -ParseHitSample(sampleText) HitSample
    }

    class Beatmap {
        +string Title
        +string Artist
        +string Creator
        +string Difficulty
        +int Mode
        +string AudioFilename

        +List~TimingPoint~ TimingPoints
        +List~HitObject~ HitObjects

        -ResolveInheritedValue(ownValue, inheritedValue) int

        +GetActiveTimingPoint(time) TimingPoint
        +GetEffectiveSampleIndex(hitObject) int
        +GetEffectiveVolume(hitObject) int
        +GetEffectiveNormalSet(hitObject) int
        +GetEffectiveAdditionSet(hitObject) int

        +GetSampleSetType(sampleSet) SampleSetType
        +GetEffectiveNormalSetType(hitObject) SampleSetType
        +GetEffectiveAdditionSetType(hitObject) SampleSetType

        +GetHitSoundTypes(hitObject) List~HitSoundType~
    }

    class TimingPoint {
        +double Time
        +double BeatLength
        +bool IsUninherited
        +double Bpm
        +int SampleSet
        +int SampleIndex
        +int Volume
    }

    class HitObject {
        +int X
        +int Y
        +int Time
        +int ObjectType
        +int HitSound
        +int EndTime

        +HitSample HitSample

        +bool IsHitCircle
        +bool IsSlider
        +bool IsSpinner
        +bool StartsNewCombo

        +bool IsNormalOnly
        +bool HasWhistle
        +bool HasFinish
        +bool HasClap
    }

    class HitSample {
        +int NormalSet
        +int AdditionSet
        +int Index
        +int Volume
        +string Filename
    }

    class SampleSetType {
        <<enumeration>>
        Default
        Normal
        Soft
        Drum
    }

    class HitSoundType {
        <<enumeration>>
        Normal
        Whistle
        Finish
        Clap
    }

    Program --> BeatmapLoader : uses
    BeatmapLoader --> Beatmap : creates

    Beatmap "1" --> "*" TimingPoint : contains
    Beatmap "1" --> "*" HitObject : contains

    HitObject "1" --> "1" HitSample : contains

    Beatmap --> SampleSetType : returns
    Beatmap --> HitSoundType : returns
```

---

## Beatmap Loading Flow

```mermaid
flowchart TD

    A[.osu file] --> B[BeatmapLoader.Load]

    B --> C[Read file lines]

    C --> D{Current section}

    D -->|General| E[Read AudioFilename and Mode]

    D -->|Metadata| F[Read Title Artist Creator Difficulty]

    D -->|TimingPoints| G[Create TimingPoint]

    D -->|HitObjects| H[Create HitObject]

    H --> I{Object type}

    I -->|HitCircle| J[Parse HitSample]
    I -->|Slider| K[Parse slider HitSample]
    I -->|Spinner| L[Read EndTime and HitSample]

    J --> M[Add HitObject to Beatmap]
    K --> M
    L --> M

    G --> N[Add TimingPoint to Beatmap]

    E --> O[Beatmap]
    F --> O
    M --> O
    N --> O
```

---

## Hitsound Inheritance

`.osu` files can use values of `0` to indicate that certain properties should inherit their values from the active `TimingPoint`.

```mermaid
flowchart TD

    A[HitObject] --> B[GetActiveTimingPoint]

    B --> C[Active TimingPoint]

    A --> D[HitSample value]

    C --> E[Inherited value]

    D --> F[ResolveInheritedValue]
    E --> F

    F --> G[Effective value]
```

This process is currently used for:

```text
SampleIndex
Volume
NormalSet
AdditionSet
```

### Example

```mermaid
flowchart LR

    A["HitSample.Index = 0"] --> C[Resolve inheritance]
    B["TimingPoint.SampleIndex = 4"] --> C

    C --> D["Effective SampleIndex = 4"]
```

---

## Sample Set Resolution

Numeric sample set values from the `.osu` format are converted into clearer internal types.

```mermaid
flowchart LR

    A["SampleSet = 0"] --> E[Default]
    B["SampleSet = 1"] --> F[Normal]
    C["SampleSet = 2"] --> G[Soft]
    D["SampleSet = 3"] --> H[Drum]
```

This conversion is handled by:

```text
GetSampleSetType()
```

The following methods combine inheritance resolution with this conversion:

```text
GetEffectiveNormalSetType()
GetEffectiveAdditionSetType()
```

---

## Hitsound Resolution

`HitSound` uses bit flags that can be combined.

`HitObject` currently exposes:

```text
HasWhistle
HasFinish
HasClap
```

`Beatmap.GetHitSoundTypes()` converts those flags into a collection of `HitSoundType` values.

```mermaid
flowchart TD

    A[HitObject.HitSound] --> B[Normal]

    A --> C{Whistle flag}
    A --> D{Finish flag}
    A --> E{Clap flag}

    C -->|true| F[Whistle]
    D -->|true| G[Finish]
    E -->|true| H[Clap]

    B --> I[List of HitSoundType]
    F --> I
    G --> I
    H --> I
```

Example:

```text
HitSound = 10

Normal
Whistle
Clap
```

---

## Current Data Flow

```mermaid
flowchart LR

    A[.osu file]

    A --> B[BeatmapLoader]

    B --> C[Beatmap]

    C --> D[TimingPoints]
    C --> E[HitObjects]

    E --> F[HitSample]

    D --> G[Inheritance resolution]
    F --> G

    G --> H[Effective SampleSet]
    G --> I[Effective SampleIndex]
    G --> J[Effective Volume]

    E --> K[HitSound flags]

    K --> L[HitSoundType]
    H --> M[SampleSetType]
```

---

## Tests

The project uses **xUnit** for automated testing.

```text
tests/
└── OsuHitsoundEditor.Tests/
    ├── BeatmapTests.cs
    ├── BeatmapLoaderTests.cs
    └── TestData/
        └── basic-beatmap.osu
```

The current test suite covers:

```text
SampleSetType conversion
HitSound flag combinations
Active TimingPoint resolution
SampleIndex inheritance
Volume inheritance
NormalSet inheritance
AdditionSet inheritance
Effective SampleSet types
Beatmap metadata parsing
TimingPoint parsing
HitObject type parsing
HitSample parsing
Spinner EndTime parsing
```

All tests can be executed from the repository root with:

```text
dotnet test
```

---

## Planned Architecture

The following components do **not exist yet**. They represent the current planned direction of the project.

```mermaid
flowchart TD

    A[BeatmapLoader] --> B[Beatmap]

    B --> C[SampleResolver]

    C --> D[Logical Hitsound Layers]

    D --> E[Timeline]
    D --> F[AudioPlayer]

    E --> G[WPF Interface]

    D --> H[SampleOptimizer]

    H --> I[osu Exporter]

    I --> J[Optimized .osu and sample files]
```

### Planned Responsibilities

`SampleResolver`

```text
Determine which samples should actually be played for each HitObject.
```

`AudioPlayer`

```text
Play the beatmap audio and synchronize hitsounds with playback.
```

`Timeline`

```text
Represent HitObjects and hitsound events over time.
```

`SampleOptimizer`

```text
Reduce duplicate sample files and efficiently assign
SampleSets and SampleIndexes during export.
```

`Exporter`

```text
Generate or modify .osu files while preserving
the original beatmap information.
```

---

## Sample Optimization Goal

One of the main goals of the project is to avoid unnecessary duplication of sample files.

For example, the same kick sample may currently need to exist under several different sample indexes:

```text
drum-hitnormal2.wav
drum-hitnormal3.wav
drum-hitnormal4.wav
```

even when all three files contain exactly the same audio.

The future `SampleOptimizer` should analyze logical samples and their combinations before assigning osu! sample indexes.

Conceptually:

```mermaid
flowchart TD

    A[Logical samples] --> B[Detect unique audio samples]

    B --> C[Analyze hitsound combinations]

    C --> D[Reuse compatible SampleSets and SampleIndexes]

    D --> E[Minimize duplicated physical files]

    E --> F[Generate osu-compatible sample names]
```

The editor should therefore avoid treating `SampleIndex` as the identity of a sound.

---

## Development Principle

The application should keep two concepts separate:

```text
Logical sample
    ↓
The actual sound the user wants to use

osu! representation
    ↓
SampleSet
SampleIndex
HitSound
Filename
```

This separation allows the editor to work with samples independently from osu!'s file naming and indexing system.

The exporter can then determine the most efficient valid representation when generating the final beatmap files.