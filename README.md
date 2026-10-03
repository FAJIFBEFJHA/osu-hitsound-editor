# osu-hitsound-editor

I started this project because manually placing every single *hitsound* in osu! eventually became boring and repetitive enough that writing an entire editor started to seem like the more reasonable option.

The idea is to create a desktop *hitsound* editor for osu!standard with a workflow similar to what you would see in FL Studio, while also using the project as an excuse to properly learn C# and .NET, even if that eventually means having to learn software testing, Git, software architecture, and everything else that usually shows up five minutes after saying, "this should be simple." Pretty dumb of me, honestly.

ChatGPT suggested developing the project "incrementally" (I have no idea what that means, but it sounds professional).

Right now, most of the work is focused on understanding `.osu` files, figuring out how *hitsounds* actually work, and building a reliable internal representation before moving on to the desktop interface.

> [!NOTE]
> This project is still in early development.
>
> The parser and core hitsound resolution systems already work and are covered by automated tests, but there is currently no fancy editor UI to click around in.
>
> In other words: the engine room exists. The buttons and blinking lights come later.

> [!NOTE]
> AI has been used during the development of this project, but not as a "generate the whole thing for me" button.
>
> ChatGPT has mostly been used to explain C# concepts, break down methods before I implemented them, review my code, help design tests, debug things when I had no idea why they were broken, research osu! behavior, discuss architecture, and generally stop me from making decisions that would probably come back to haunt me three phases later.
>
> The actual production code is still something I write myself.

## Current Status

Hitsound Resolution is complete. The project is currently being prepared for public development, with Physical Sample Resolution planned next.

Current checkpoint:

```text
100/100 automated tests passing
```

Implemented:

- `.osu` file format version parsing
- General, Editor, Metadata, Difficulty, TimingPoints, and HitObjects parsing
- Hit circles, sliders, slider edges, and spinners
- Hit sample parsing
  - NormalSet
  - AdditionSet
  - SampleIndex
  - Volume
  - Filename
- Hitsound bit flag interpretation
  - Normal
  - Whistle
  - Finish
  - Clap
- Sample set representation
  - Default
  - Normal
  - Soft
  - Drum
- Active timing point resolution
  - active timing point
  - active uninherited timing point
  - active inherited timing point
- Effective slider velocity resolution
- Hitsound inheritance resolution
  - SampleIndex
  - Volume
  - NormalSet
  - AdditionSet
  - beatmap default sample set
- Logical `HitSoundLayer` generation
- Slider timing
  - span duration
  - total duration
  - real head/repeat/tail times
- Slider edge hitsound resolution using the timing point active at the edge's real time
- Continuous slider body hitsounds
  - `SliderSlide`
  - `SliderWhistle`
- Slider tick behavior
  - tick distance
  - pre-v8 / v8+ compatibility
  - tick times across normal and reversed spans
  - `SliderTick` hitsound layers
- Automated tests with xUnit

Physical sample-file discovery and resolution have not been implemented yet. That is planned for the Physical Sample Resolution stage.

## Project Structure

```text
osu-hitsound-editor/
├── .gitignore
├── CONTRIBUTING.md
├── LICENSE
├── README.md
├── OsuHitsoundEditor.slnx
│
├── src/
│   └── OsuHitsoundEditor/
│       ├── Beatmap.cs
│       ├── BeatmapLoader.cs
│       ├── HitObject.cs
│       ├── HitSample.cs
│       ├── HitSoundLayer.cs
│       ├── HitSoundType.cs
│       ├── SampleSetType.cs
│       ├── SliderEdge.cs
│       ├── TimingPoint.cs
│       ├── Program.cs
│       └── OsuHitsoundEditor.csproj
│
├── tests/
│   └── OsuHitsoundEditor.Tests/
│       ├── BeatmapTests.cs
│       ├── BeatmapLoaderTests.cs
│       ├── TestOrganizationTests.cs
│       ├── OsuHitsoundEditor.Tests.csproj
│       └── TestData/
│           └── basic-beatmap.osu
│
└── docs/
    ├── AI_WORKFLOW.md
    ├── DECISIONS.md
    ├── PROJECT_CONTEXT.md
    ├── ROADMAP.md
    ├── WORKFLOW.md
    └── architecture.md
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

Current checkpoint:

```text
100/100 tests passing
```

The test suite is also executed automatically through GitHub Actions on pushes and pull requests targeting `main`.

The test suite currently covers:

- `.osu` parsing
- beatmap settings and metadata
- timing point resolution
- inherited and uninherited timing behavior
- effective sample sets, sample indexes, and volume
- hitsound flag and logical layer resolution
- slider span, duration, and edge timing
- slider edge hitsounds
- continuous slider body hitsounds
- slider tick distance and format-version behavior
- slider tick timing
- slider tick hitsound layers
- relevant invalid-state and boundary cases

## Development Roadmap

### Core beatmap model

- [x] Metadata parsing
- [x] General and Editor settings
- [x] Difficulty settings
- [x] Timing point parsing
- [x] Hit object parsing
- [x] HitSample parsing
- [x] Slider and slider-edge parsing
- [x] Spinner parsing
- [x] HitSound flag interpretation
- [x] Sample inheritance resolution
- [x] SampleSet type resolution
- [x] Automated tests

### Hitsound resolution

- [x] Resolve logical hitsound layers
- [x] Resolve active inherited and uninherited timing points
- [x] Resolve effective slider velocity
- [x] Resolve slider head, repeat, and tail timing
- [x] Resolve slider edge hitsounds
- [x] Resolve continuous `SliderSlide`
- [x] Resolve continuous `SliderWhistle`
- [x] Resolve slider tick distance and timing
- [x] Resolve `SliderTick` hitsound layers
- [ ] Resolve actual physical sample files

### Physical sample resolution

Planned next:

- [ ] Discover sample files used by the beatmap
- [ ] Resolve osu! sample filenames
- [ ] Separate logical sample identity from physical `.osu` representation
- [ ] Define a stable logical `SampleId`
- [ ] Handle missing samples predictably
- [ ] Add sample-resolution tests

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

For the detailed development plan and stage ordering, see [`docs/ROADMAP.md`](docs/ROADMAP.md).

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

The current project separates:

- `.osu` parsing in `BeatmapLoader`
- beatmap data and timing/hitsound resolution in `Beatmap`
- hit object, hit sample, slider edge, and timing-point data
- logical hitsound output through `HitSoundLayer`

The project intentionally keeps logical hitsound identity separate from the physical `SampleSet`, `SampleIndex`, and filename representation used by osu!.

Future stages are expected to introduce responsibilities such as:

```text
Physical sample resolution
AudioPlayer
Timeline
SampleOptimizer
Exporter
```

These components and the current data flow are documented in more detail in [`docs/architecture.md`](docs/architecture.md).

## Contributing

Contributions, bug reports, suggestions, and documentation improvements are welcome.

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for development and contribution guidelines.

## License

This project is licensed under the MIT License.

See [`LICENSE`](LICENSE) for details.

The MIT License applies to the source code of this repository. osu! and any third-party names, trademarks, formats, or assets remain the property of their respective owners.

## Disclaimer

This is a personal learning project.

It is not an official osu! project, it is not affiliated with ppy, and nobody should treat the current architecture as a reference for how a professional project should be built. This is, first and foremost, a learning project.

A large part of the project will keep changing as I learn more about C#, .NET, software design, testing, and the osu! format itself.

Some decisions that seem completely logical to me right now may look absolutely stupid six months from now.

So if you find old code that is simply shit, there is a very good chance I was learning that exact concept when I wrote it.

If you find current code that looks questionable, please let me know and give me at least a little time to fix it before telling me to go fuck myself.

## Learning Project and AI Assistance

If you made it this far and also decided to read `AI_WORKFLOW.md`, `DECISIONS.md`, `PROJECT_CONTEXT.md`, `ROADMAP.md`, or `WORKFLOW.md`, you probably already noticed that ChatGPT was pretty heavily involved in the development of this project.

And yes, a lot of the time the AI told me what responsibility a method should have, what data it should receive, what it should return, and gave me pseudocode that was fairly close to the final implementation. It also wrote a large portion of the complete tests, helped me find bugs, researched osu! behavior I didn't know about, and was involved quite a bit in architecture decisions.

I'm not going to pretend I sat in front of a blank screen and figured absolutely everything out on my own, because that simply isn't what happened.

My part was understanding what we were trying to do, turning that flow into C# code, connecting it with the parts that already existed, figuring out what was wrong when something failed, and being able to explain afterward why it worked.

Some people will probably consider that too much help from an AI, and that's fine. That's exactly why those documents exist in the repository: I'd rather let people see exactly how I developed the project than try to make it look like I learned C# locked inside a cave with no internet.

If, after reading the workflow, your conclusion is that ChatGPT basically held my hand through the entire project, congratulations, you discovered something I already wrote a few paragraphs above.

If, to you, that only counts as "writing the syntax the AI told you to write," I'm still not going to send a hitman to your house for saying it. Probably.