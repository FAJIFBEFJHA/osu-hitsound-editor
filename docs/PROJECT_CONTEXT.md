# PROJECT_CONTEXT

Last updated: 2026-10-06

## Project

`osu-hitsound-editor`

C#/.NET learning project focused on building an osu!standard hitsound editor with a DAW-like workflow.

Repository:

```text
https://github.com/FAJIFBEFJHA/osu-hitsound-editor
```

The repository is public.

## Current stage

**Audio Infrastructure**

Current test status:

```text
200/200 passing
```

Beatmap Parsing and Base Model, Hitsound Resolution, and the current Physical Sample Resolution boundary are complete.

## Current objective

Continue production Audio Infrastructure from the now-established WASAPI output/mixer lifecycle.

The current production audio boundary can:

- open supported physical audio files
- decode WAV/MP3 through `AudioFileReader`
- decode OGG through `VorbisWaveReader`
- convert decoded audio to `ISampleProvider`
- normalize mono to stereo
- normalize multichannel audio to stereo by preserving input channels 0 and 1
- resample to a caller-provided target sample rate
- explicitly initialize a `WasapiPlayer` output device
- create a persistent stereo float `MixingSampleProvider` at `DeviceMixFormat.SampleRate`
- keep output initialization out of the constructor
- play, pause, and stop an initialized output device
- dispose the owned output device

The next production objective is to add an actual audio source to the persistent mixer using the existing `OpenAudioFile(...)` and `NormalizeForMixer(...)` pipeline, while defining the source-reader lifetime deliberately.

## Current implementation checkpoint

### Logical hitsound resolution

The logical layer is complete and remains the source of audible hitsound intent.

Relevant behavior includes:

- effective `SampleSet`
- effective addition sample set
- effective `SampleIndex`
- effective `Volume`
- standard hitobject hitsounds
- slider edge hitsounds
- slider body `SliderSlide`
- slider body `SliderWhistle`
- slider tick timing
- slider tick hitsounds
- explicit custom filename representation

Slider edge, body, and tick responsibilities remain separate. There is no `GetAllSliderHitSounds()` API.

### Legacy slider edge inheritance

The remaining `edgeSets = 0:0` question has been resolved.

Canonical behavior:

```text
explicit slider edge NormalSet = 0
    -> unspecified
    -> inherit from the sample timing point at the edge time

explicit slider edge AdditionSet = 0
    -> inherit the resolved effective NormalSet

edge time before the first timing point
    -> use the first timing point in the beatmap
```

The slider-level trailing `HitSample.NormalSet` / `HitSample.AdditionSet` do not override an explicit edge `0:0` in this legacy path.

`GetEffectiveNormalSet(SliderEdge sliderEdge, double time)` now uses `GetSampleTimingPoint(time)` so pre-first-timing-point behavior matches the canonical legacy sample-point fallback.

### Physical sample resolution boundary

`SampleResolver` owns physical osu! sample lookup behavior.

Implemented methods include:

```text
GetStandardSampleLookup(HitSoundLayer layer)
GetSliderBodySampleLookup(HitSoundLayer layer)
GetSliderTickSampleLookup(HitSoundLayer layer)
ResolveCustomSamplePath(HitSoundLayer layer, string beatmapDirectory)
ResolveBeatmapSamplePath(string? beatmapFilename, string beatmapDirectory)
ResolveSample(HitSoundLayer layer, string beatmapDirectory)
```

Standard/slider lookup methods now return:

```text
BeatmapFilename
BeatmapUniversalFilename
FallbackFilename
```

### Physical sample lookup order

For `SampleIndex >= 1`, the deterministic beatmap lookup chain is:

```text
specific banked beatmap sample
    -> universal bankless beatmap sample
    -> external/user-skin fallback required
```

Examples of universal bankless names:

```text
hitnormal.wav
hitwhistle.wav
hitfinish.wav
hitclap.wav
sliderslide.wav
sliderwhistle.wav
slidertick.wav
```

For `SampleIndex = 0`:

```text
BeatmapFilename = null
BeatmapUniversalFilename = null
external fallback remains available
```

Therefore, `SampleIndex = 0` must not accidentally consume either banked or universal beatmap samples.

For a custom index greater than 1, an unindexed banked beatmap filename such as `normal-hitnormal.wav` is not a beatmap fallback for `normal-hitnormal2.wav`; the universal bankless candidate is used instead.

### Beatmap sample extensions

Legacy beatmap sample extension lookup remains:

```text
.wav
.mp3
.ogg
```

The first existing extension wins.

### Physical resolution result model

The existing outcomes remain sufficient:

```text
BeatmapSampleFound
ExternalFallbackRequired
CustomSampleFound
CustomSampleMissing
```

A universal bankless beatmap sample still produces `BeatmapSampleFound`; no extra outcome was introduced.

### Deferred Physical Sample Resolution work

The following are deliberately deferred rather than blockers for Audio Infrastructure:

- stable logical `SampleId`
- loading additional sample metadata before a concrete consumer requires it
- broader refactoring of duplicated standard/body/tick lookup code

Do not introduce these merely to make the current boundary look more abstract.

## Audio Infrastructure checkpoint

### Platform and dependencies

Production and test projects target:

```text
net10.0-windows
```

Current audio dependencies:

```text
NAudio 3.1.0
NAudio.Vorbis 3.0.0
```

`NAudio.SoundFile` / libsndfile is not currently required.

### Diagnostic spike findings

The temporary `tools/AudioProbe` spike was used only to answer infrastructure questions and has been removed.

Validated experimentally before removal:

- WAV, MP3, and OGG decoding
- duration and decoder seek
- `WasapiPlayer` basic play/pause/resume/stop
- seek during playback
- simultaneous hitsound mixing
- mono/stereo/sample-rate normalization
- multichannel handling
- per-layer linear volume with `VolumeSampleProvider`
- low-latency WASAPI behavior
- rendered output position as the playback timeline basis

Spike code is evidence, not production implementation. It must not be copied into `src/`; production code is reconstructed by the author through the normal learning workflow.

### Current production class

`AudioPlaybackEngine` currently owns:

```text
WasapiPlayer? outputDevice
MixingSampleProvider? mixer
```

and contains:

```text
InitializeOutput() -> void
GetInitializedOutputDevice() -> WasapiPlayer
Play() -> void
Pause() -> void
Stop() -> void
OpenAudioFile(string filePath) -> WaveStream
NormalizeForMixer(WaveStream reader, int targetSampleRate) -> ISampleProvider
Dispose() -> void
```

`InitializeOutput()`:

```text
if already initialized
    -> InvalidOperationException

WasapiPlayerBuilder
    -> WithLowLatency()
    -> Build()
    -> WasapiPlayer

target sample rate
    -> outputDevice.DeviceMixFormat.SampleRate

mixer format
    -> IEEE float
    -> target device sample rate
    -> 2 channels

mixer
    -> persistent MixingSampleProvider
    -> ReadFully = true

outputDevice.Init(mixer)
```

Initialization is explicit rather than constructor-driven. Constructing `AudioPlaybackEngine` alone does not open a physical audio device, so decoding/normalization tests remain hardware-independent.

`GetInitializedOutputDevice()` centralizes the shared precondition used by `Play()`, `Pause()`, and `Stop()`:

```text
outputDevice == null
    -> InvalidOperationException

otherwise
    -> return outputDevice
```

`Dispose()` releases the owned `WasapiPlayer` and clears the persistent output/mixer references.

`OpenAudioFile(...)`:

```text
missing file
    -> FileNotFoundException

.wav / .mp3
    -> AudioFileReader

.ogg
    -> VorbisWaveReader

other extension
    -> NotSupportedException
```

`NormalizeForMixer(...)`:

```text
WaveStream
    -> ToSampleProvider()

mono
    -> MonoToStereoSampleProvider

more than 2 channels
    -> MultiplexingSampleProvider
    -> input 0 -> output 0
    -> input 1 -> output 1

sample rate differs from target
    -> WdlResamplingSampleProvider

return current ISampleProvider
```

The next input added to `mixer` must use the mixer's/device's sample rate rather than a hard-coded rate.

### Verification boundary

Automated tests currently cover the hardware-independent `Play()`, `Pause()`, and `Stop()` error contract when output has not been initialized.

`InitializeOutput()` itself opens a real WASAPI device, so ordinary CI does not currently exercise that device-dependent path. Real-device playback will be verified with a focused manual/integration check once a production source can actually be added to the mixer.

## Test progression

Recent checkpoints:

```text
182/182  legacy slider sample-source semantics and fallback coverage
185/185  edgeSets = 0:0 / pre-first-timing-point inheritance coverage
190/190  universal bankless beatmap sample fallback coverage
193/193  audio file opening/error coverage
197/197  audio format normalization coverage
198/198  Play() uninitialized-output contract
199/199  Pause() uninitialized-output contract
200/200  Stop() uninitialized-output contract
```

Treat the actual source and test suite as the implementation truth if this list becomes stale.

## Workflow tooling

A repository `.editorconfig` remains present for naming/style alignment.

Session closing review is automated through:

```text
scripts/New-SessionReview.ps1
```

The script runs the relevant verification commands and writes one review report outside the repository. The report is uploaded/reviewed instead of asking the user to navigate a large terminal `git diff`.

## Important / technical debt

Do not mix these into the next audio step unless they become blockers:

- define stable logical `SampleId` only when a concrete identity consumer requires it
- load only audio/sample metadata that a concrete editor/playback requirement needs
- revisit `GetHitSoundLayers(SliderEdge, HitSample, double time)` signature only during a deliberate API review
- use `CultureInfo.InvariantCulture` consistently when parser robustness work resumes
- improve malformed/empty parser-field handling later
- keep the lazer-specific spinner traversal sound deferred from the canonical legacy model
- handle partial `InitializeOutput()` failure/cleanup deliberately when output-device error handling is implemented
- define ownership/disposal for decoded `WaveStream` instances once sources are added to the persistent mixer
- do not introduce global optimization before deterministic export/reload equivalence exists

## Exact next task

Define and implement the smallest production responsibility that adds one real audio source to the initialized persistent mixer:

```text
file path
    -> OpenAudioFile(...)
    -> WaveStream

mixer.WaveFormat.SampleRate
    -> NormalizeForMixer(...)

normalized ISampleProvider
    -> mixer.AddMixerInput(...)
```

Before finalizing that method, make the source-reader ownership/lifetime explicit so opened `WaveStream` instances are not leaked after mixer inputs finish.

Keep the automated suite hardware-independent; use a focused manual playback check only after the production pipeline can actually feed audio into the WASAPI mixer.

## Source-of-truth order

When sources disagree, use this order:

1. Tests and actual source code
2. Official osu! documentation / official open-source implementation for format behavior
3. `docs/DECISIONS.md`
4. `docs/PROJECT_CONTEXT.md`
5. `docs/ROADMAP.md`
6. Chat history or AI suggestions
